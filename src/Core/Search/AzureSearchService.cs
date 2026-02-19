using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

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
    private static readonly (string ResourceType, string DisplayName, ImageMoniker Icon)[] SearchableResourceTypes =
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

        var accounts = AzureAuthService.Instance.Accounts;
        if (accounts.Count == 0)
            return 0;

        // First, collect all subscriptions across all accounts/tenants in parallel
        var subscriptionInfos = new ConcurrentBag<SubscriptionSearchInfo>();
        var tenantTasks = new List<Task>();

        foreach (var account in accounts)
        {
            tenantTasks.Add(CollectSubscriptionsForAccountAsync(account, subscriptionInfos, cancellationToken));
        }

        await Task.WhenAll(tenantTasks).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var allSubscriptions = subscriptionInfos.ToArray();
        uint totalSubscriptions = (uint)allSubscriptions.Length;
        int subscriptionsSearched = 0;
        int totalResults = 0;

        onProgress?.Invoke(0, totalSubscriptions);

        // Search all subscriptions in parallel with controlled concurrency
        var semaphore = new SemaphoreSlim(Environment.ProcessorCount * 2);
        var searchTasks = new List<Task>();

        foreach (var subInfo in allSubscriptions)
        {
            searchTasks.Add(SearchSubscriptionAsync(
                subInfo,
                searchText,
                result =>
                {
                    Interlocked.Increment(ref totalResults);
                    onResultFound?.Invoke(result);
                },
                () =>
                {
                    int searched = Interlocked.Increment(ref subscriptionsSearched);
                    onProgress?.Invoke((uint)searched, totalSubscriptions);
                },
                semaphore,
                cancellationToken));
        }

        await Task.WhenAll(searchTasks).ConfigureAwait(false);

        return (uint)totalResults;
    }

    private async Task CollectSubscriptionsForAccountAsync(
        AccountInfo account,
        ConcurrentBag<SubscriptionSearchInfo> subscriptionInfos,
        CancellationToken cancellationToken)
    {
        try
        {
            var tenants = new List<TenantInfo>();
            await foreach (var tenant in AzureResourceService.Instance.GetTenantsAsync(account.AccountId, cancellationToken))
            {
                tenants.Add(tenant);
            }

            // Query subscriptions for each tenant in parallel
            var tenantSubTasks = tenants.Select(async tenant =>
            {
                try
                {
                    await foreach (var sub in AzureResourceService.Instance.GetSubscriptionsForTenantAsync(
                        account.AccountId, tenant.TenantId, cancellationToken))
                    {
                        subscriptionInfos.Add(new SubscriptionSearchInfo(
                            account.AccountId,
                            account.Username,
                            sub.Data.SubscriptionId,
                            sub.Data.DisplayName));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Search: Failed to get subscriptions for tenant {tenant.TenantId}: {ex.Message}");
                }
            });

            await Task.WhenAll(tenantSubTasks).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Search: Failed to get tenants for account {account.Username}: {ex.Message}");
        }
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
            ArmClient client = AzureResourceService.Instance.GetClient(subInfo.SubscriptionId);
            SubscriptionResource sub = client.GetSubscriptionResource(
                SubscriptionResource.CreateResourceIdentifier(subInfo.SubscriptionId));

            // Search all resource types in parallel within this subscription
            var resourceTypeTasks = SearchableResourceTypes.Select(async rt =>
            {
                try
                {
                    // Include properties for creating proper nodes
                    string filter = $"resourceType eq '{rt.ResourceType}'";

                    await foreach (GenericResource resource in sub.GetGenericResourcesAsync(filter: filter, expand: "properties", cancellationToken: cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        string resourceName = resource.Data.Name;

                        // Case-insensitive substring match
                        if (resourceName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            string resourceGroup = resource.Id.ResourceGroupName;

                            // Create the actual resource node for this result
                            ExplorerNodeBase actualNode = CreateResourceNode(
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
    /// Creates the appropriate resource node based on the resource type.
    /// </summary>
    private static ExplorerNodeBase CreateResourceNode(
        string resourceType,
        string name,
        string subscriptionId,
        string resourceGroup,
        GenericResource resource)
    {
        try
        {
            return resourceType switch
            {
                "Microsoft.Web/sites" => CreateWebSiteNode(name, subscriptionId, resourceGroup, resource),
                "Microsoft.Web/serverfarms" => CreateAppServicePlanNode(name, subscriptionId, resourceGroup, resource),
                "Microsoft.Storage/storageAccounts" => CreateStorageAccountNode(name, subscriptionId, resourceGroup, resource),
                "Microsoft.KeyVault/vaults" => CreateKeyVaultNode(name, subscriptionId, resourceGroup, resource),
                "Microsoft.Sql/servers" => CreateSqlServerNode(name, subscriptionId, resourceGroup, resource),
                "Microsoft.Compute/virtualMachines" => CreateVirtualMachineNode(name, subscriptionId, resourceGroup, resource),
                "Microsoft.Cdn/profiles/endpoints" => CreateFrontDoorNode(name, subscriptionId, resourceGroup, resource),
                _ => null
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Search: Failed to create node for {resourceType}/{name}: {ex.Message}");
            return null;
        }
    }

    private static ExplorerNodeBase CreateWebSiteNode(string name, string subscriptionId, string resourceGroup, GenericResource resource)
    {
        string state = null;
        string defaultHostName = null;

        if (resource.Data.Properties != null)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(resource.Data.Properties);
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("state", out JsonElement stateElement))
                    state = stateElement.GetString();

                if (root.TryGetProperty("defaultHostName", out JsonElement hostElement))
                    defaultHostName = hostElement.GetString();

                // Check if it's a Function App (kind contains "functionapp")
                string kind = resource.Data.Kind;
                if (!string.IsNullOrEmpty(kind) && kind.IndexOf("functionapp", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return new FunctionAppNode(name, subscriptionId, resourceGroup, state, defaultHostName);
                }
            }
            catch { }
        }

        return new AppServiceNode(name, subscriptionId, resourceGroup, state, defaultHostName);
    }

    private static AppServicePlanNode CreateAppServicePlanNode(string name, string subscriptionId, string resourceGroup, GenericResource resource)
    {
        string sku = resource.Data.Sku?.Name;
        string kind = resource.Data.Kind;
        int? numberOfSites = null;

        if (resource.Data.Properties != null)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(resource.Data.Properties);
                if (doc.RootElement.TryGetProperty("numberOfSites", out JsonElement sitesElement))
                    numberOfSites = sitesElement.GetInt32();
            }
            catch { }
        }

        return new AppServicePlanNode(name, subscriptionId, resourceGroup, sku, kind, numberOfSites);
    }

    private static StorageAccountNode CreateStorageAccountNode(string name, string subscriptionId, string resourceGroup, GenericResource resource)
    {
        string state = null;
        string kind = resource.Data.Kind;
        string skuName = resource.Data.Sku?.Name;

        if (resource.Data.Properties != null)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(resource.Data.Properties);
                if (doc.RootElement.TryGetProperty("provisioningState", out JsonElement stateElement))
                    state = stateElement.GetString();
            }
            catch { }
        }

        return new StorageAccountNode(name, subscriptionId, resourceGroup, state, kind, skuName);
    }

    private static KeyVaultNode CreateKeyVaultNode(string name, string subscriptionId, string resourceGroup, GenericResource resource)
    {
        string state = null;
        string vaultUri = null;

        if (resource.Data.Properties != null)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(resource.Data.Properties);

                if (doc.RootElement.TryGetProperty("provisioningState", out JsonElement stateElement))
                    state = stateElement.GetString();

                if (doc.RootElement.TryGetProperty("vaultUri", out JsonElement uriElement))
                    vaultUri = uriElement.GetString();
            }
            catch { }
        }

        return new KeyVaultNode(name, subscriptionId, resourceGroup, state, vaultUri);
    }

    private static SqlServerNode CreateSqlServerNode(string name, string subscriptionId, string resourceGroup, GenericResource resource)
    {
        string state = null;
        string fqdn = null;

        if (resource.Data.Properties != null)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(resource.Data.Properties);

                if (doc.RootElement.TryGetProperty("state", out JsonElement stateElement))
                    state = stateElement.GetString();

                if (doc.RootElement.TryGetProperty("fullyQualifiedDomainName", out JsonElement fqdnElement))
                    fqdn = fqdnElement.GetString();
            }
            catch { }
        }

        return new SqlServerNode(name, subscriptionId, resourceGroup, state, fqdn);
    }

    private static VirtualMachineNode CreateVirtualMachineNode(string name, string subscriptionId, string resourceGroup, GenericResource resource)
    {
        // Power state and IPs require instance view which isn't in generic resource properties
        // They will be loaded when the node is expanded
        return new VirtualMachineNode(name, subscriptionId, resourceGroup, null, null, null, null, null);
    }

    private static FrontDoorNode CreateFrontDoorNode(string name, string subscriptionId, string resourceGroup, GenericResource resource)
    {
        string state = null;
        string hostName = null;

        if (resource.Data.Properties != null)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(resource.Data.Properties);

                if (doc.RootElement.TryGetProperty("enabledState", out JsonElement stateElement))
                    state = stateElement.GetString();

                if (doc.RootElement.TryGetProperty("hostName", out JsonElement hostElement))
                    hostName = hostElement.GetString();
            }
            catch { }
        }

        return new FrontDoorNode(name, subscriptionId, resourceGroup, state, hostName);
    }

    private sealed class SubscriptionSearchInfo
    {
        public SubscriptionSearchInfo(string accountId, string accountName, string subscriptionId, string subscriptionName)
        {
            AccountId = accountId;
            AccountName = accountName;
            SubscriptionId = subscriptionId;
            SubscriptionName = subscriptionName;
        }

        public string AccountId { get; }
        public string AccountName { get; }
        public string SubscriptionId { get; }
        public string SubscriptionName { get; }
    }
}
