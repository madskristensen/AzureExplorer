using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;

using AzureExplorer.AppService.Models;
using AzureExplorer.AppServicePlan.Models;
using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;
using AzureExplorer.FrontDoor.Models;
using AzureExplorer.FunctionApp.Models;
using AzureExplorer.KeyVault.Models;
using AzureExplorer.Sql.Models;
using AzureExplorer.Storage.Models;
using AzureExplorer.VirtualMachine.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Core.Search;

/// <summary>
/// Service for searching Azure resources across all logged-in accounts in parallel.
/// Results are streamed as SearchResultNode instances for tree view display.
/// </summary>
internal sealed class AzureSearchService
{
    private static readonly Lazy<AzureSearchService> _instance = new(() => new AzureSearchService());

    private AzureSearchService() { }

    public static AzureSearchService Instance => _instance.Value;

    /// <summary>
    /// Resource types to search with their display names and icons.
    /// </summary>
    private static readonly (string ResourceType, string DisplayName, ImageMoniker Icon)[] _searchableResourceTypes =
    {
        ("Microsoft.Web/sites", "App Service", KnownMonikers.Web),
        ("Microsoft.Web/serverfarms", "App Service Plan", KnownMonikers.ApplicationGroup),
        ("Microsoft.Storage/storageAccounts", "Storage Account", KnownMonikers.AzureStorageAccount),
        ("Microsoft.KeyVault/vaults", "Key Vault", KnownMonikers.Key),
        ("Microsoft.Sql/servers", "SQL Server", KnownMonikers.Database),
        ("Microsoft.Compute/virtualMachines", "Virtual Machine", KnownMonikers.VirtualMachine),
        ("Microsoft.Cdn/profiles/endpoints", "Front Door", KnownMonikers.CloudGroup),
    };

    /// <summary>
    /// Searches all Azure resources across all accounts, streaming SearchResultNode instances for tree display.
    /// </summary>
    /// <param name="searchText">The text to search for (case-insensitive substring match on resource name).</param>
    /// <param name="onResultFound">Callback invoked on each result found (may be called from multiple threads).</param>
    /// <param name="onProgress">Callback for progress updates (subscriptionsSearched, totalSubscriptions).</param>
    /// <param name="cancellationToken">Token to cancel the search.</param>
    /// <returns>Total number of results found.</returns>
    public async Task<uint> SearchAllResourcesAsync(
        string searchText,
        Action<SearchResultNode> onResultFound,
        Action<uint, uint> onProgress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return 0;

        IReadOnlyList<AccountInfo> accounts = AzureAuthService.Instance.Accounts;
        if (accounts.Count == 0)
            return 0;

        var totalResults = 0;
        var subscriptionsSearched = 0;
        var totalSubscriptionsEstimate = 0;

        // Higher concurrency for network-bound I/O operations
        var semaphore = new SemaphoreSlim(16);
        var searchTasks = new ConcurrentBag<Task>();
        var allTasksStarted = new TaskCompletionSource<bool>();

        // Stream-start: begin searching subscriptions as they're discovered (no two-phase wait)
        // Use silent credentials to avoid interactive authentication prompts during search
        IEnumerable<Task> collectAndSearchTasks = accounts.Select(async account =>
        {
            try
            {
                // Collect tenants for this account using silent auth (no prompts)
                var tenants = new List<TenantInfo>();
                await foreach (TenantInfo tenant in AzureResourceService.Instance.GetTenantsForSearchAsync(account.AccountId, cancellationToken))
                {
                    tenants.Add(tenant);
                }

                // Process tenants in parallel
                IEnumerable<Task> tenantTasks = tenants.Select(async tenant =>
                {
                    try
                    {
                        // Use silent auth to avoid prompts during search
                        await foreach (SubscriptionResource sub in AzureResourceService.Instance.GetSubscriptionsForTenantForSearchAsync(
                            account.AccountId, tenant.TenantId, cancellationToken))
                        {
                            Interlocked.Increment(ref totalSubscriptionsEstimate);

                            var subInfo = new SubscriptionSearchInfo(
                                account.AccountId,
                                account.Username,
                                sub.Data.SubscriptionId,
                                sub.Data.DisplayName,
                                tenant.TenantId);

                            // Start searching this subscription immediately (don't wait for all subs)
                            Task searchTask = SearchSubscriptionAsync(
                                subInfo,
                                searchText,
                                result =>
                                {
                                    Interlocked.Increment(ref totalResults);
                                    onResultFound?.Invoke(result);
                                },
                                () =>
                                {
                                    var searched = Interlocked.Increment(ref subscriptionsSearched);
                                    onProgress?.Invoke((uint)searched, (uint)Math.Max(totalSubscriptionsEstimate, searched));
                                },
                                semaphore,
                                cancellationToken);

                            searchTasks.Add(searchTask);
                        }
                    }
                    catch (AuthenticationRequiredException)
                    {
                        // Silent auth failed - skip this tenant without prompting
                        System.Diagnostics.Debug.WriteLine($"Search: Skipping tenant {tenant.TenantId} - authentication required");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Search: Failed to get subscriptions for tenant {tenant.TenantId}: {ex.Message}");
                    }
                });

                await Task.WhenAll(tenantTasks).ConfigureAwait(false);
            }
            catch (AuthenticationRequiredException)
            {
                // Silent auth failed - skip this account without prompting
                System.Diagnostics.Debug.WriteLine($"Search: Skipping account {account.Username} - authentication required");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Search: Failed to get tenants for account {account.Username}: {ex.Message}");
            }
        });

        // Wait for all collection tasks to start their search tasks
        await Task.WhenAll(collectAndSearchTasks).ConfigureAwait(false);

        // Wait for all search tasks to complete
        await Task.WhenAll(searchTasks).ConfigureAwait(false);

        return (uint)totalResults;
    }

    private async Task SearchSubscriptionAsync(
        SubscriptionSearchInfo subInfo,
        string searchText,
        Action<SearchResultNode> onResultFound,
        Action onSubscriptionComplete,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Use silent client to avoid interactive authentication prompts during search
            ArmClient client = AzureResourceService.Instance.GetSilentClient(subInfo.AccountId, subInfo.TenantId);
            SubscriptionResource sub = client.GetSubscriptionResource(
                SubscriptionResource.CreateResourceIdentifier(subInfo.SubscriptionId));

            // Search all resource types in parallel within this subscription
            IEnumerable<Task> resourceTypeTasks = _searchableResourceTypes.Select(async rt =>
            {
                try
                {
                    // Don't expand properties - saves significant API overhead
                    // Properties will load lazily when user expands the node
                    var filter = $"resourceType eq '{rt.ResourceType}'";

                    await foreach (GenericResource resource in sub.GetGenericResourcesAsync(filter: filter, cancellationToken: cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var resourceName = resource.Data.Name;

                        // Case-insensitive substring match
                        if (resourceName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            var resourceGroup = resource.Id.ResourceGroupName;

                            // Create a lightweight resource node (properties load on expand)
                            ExplorerNodeBase actualNode = CreateResourceNodeLightweight(
                                rt.ResourceType,
                                resourceName,
                                subInfo.SubscriptionId,
                                resourceGroup,
                                resource);

                            var resultNode = new SearchResultNode(
                                resourceName,
                                rt.DisplayName,
                                resource.Id.ToString(),
                                subInfo.SubscriptionId,
                                subInfo.SubscriptionName,
                                subInfo.AccountName,
                                rt.Icon,
                                actualNode);

                            onResultFound?.Invoke(resultNode);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (AuthenticationRequiredException)
                {
                    // Silent auth failed - skip this resource type without prompting
                    System.Diagnostics.Debug.WriteLine($"Search: Skipping {rt.ResourceType} in {subInfo.SubscriptionId} - authentication required");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Search: Failed to search {rt.ResourceType} in {subInfo.SubscriptionId}: {ex.Message}");
                }
            });

            await Task.WhenAll(resourceTypeTasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (AuthenticationRequiredException)
        {
            // Silent auth failed - skip this subscription without prompting
            System.Diagnostics.Debug.WriteLine($"Search: Skipping subscription {subInfo.SubscriptionId} - authentication required");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Search: Failed to search subscription {subInfo.SubscriptionId}: {ex.Message}");
        }
        finally
        {
            semaphore.Release();
            onSubscriptionComplete?.Invoke();
        }
    }

    /// <summary>
    /// Creates a lightweight resource node without full properties (faster for search).
    /// </summary>
    private static ExplorerNodeBase CreateResourceNodeLightweight(
        string resourceType,
        string name,
        string subscriptionId,
        string resourceGroup,
        GenericResource resource)
    {
        try
        {
            // Create nodes with minimal data - properties will load on expand
            return resourceType switch
            {
                "Microsoft.Web/sites" => CreateWebSiteNodeLightweight(name, subscriptionId, resourceGroup, resource),
                "Microsoft.Web/serverfarms" => new AppServicePlanNode(name, subscriptionId, resourceGroup, resource.Data.Sku?.Name, resource.Data.Kind, null),
                "Microsoft.Storage/storageAccounts" => new StorageAccountNode(name, subscriptionId, resourceGroup, null, resource.Data.Kind, resource.Data.Sku?.Name),
                "Microsoft.KeyVault/vaults" => new KeyVaultNode(name, subscriptionId, resourceGroup, null, null),
                "Microsoft.Sql/servers" => new SqlServerNode(name, subscriptionId, resourceGroup, null, null),
                "Microsoft.Compute/virtualMachines" => new VirtualMachineNode(name, subscriptionId, resourceGroup, null, null, null, null, null),
                "Microsoft.Cdn/profiles/endpoints" => new FrontDoorNode(name, subscriptionId, resourceGroup, null, null),
                _ => null
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Search: Failed to create node for {resourceType}/{name}: {ex.Message}");
            return null;
        }
    }

    private static ExplorerNodeBase CreateWebSiteNodeLightweight(string name, string subscriptionId, string resourceGroup, GenericResource resource)
    {
        // Check if it's a Function App (kind contains "functionapp")
        var kind = resource.Data.Kind;
        if (!string.IsNullOrEmpty(kind) && kind.IndexOf("functionapp", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return new FunctionAppNode(name, subscriptionId, resourceGroup, null, null);
        }
        return new AppServiceNode(name, subscriptionId, resourceGroup, null, null);
    }

    private sealed class SubscriptionSearchInfo
    {
        public SubscriptionSearchInfo(string accountId, string accountName, string subscriptionId, string subscriptionName, string tenantId)
        {
            AccountId = accountId;
            AccountName = accountName;
            SubscriptionId = subscriptionId;
            SubscriptionName = subscriptionName;
            TenantId = tenantId;
        }

        public string AccountId { get; }
        public string AccountName { get; }
        public string SubscriptionId { get; }
        public string SubscriptionName { get; }
        public string TenantId { get; }
    }
}
