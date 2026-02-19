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
    [
        ("Microsoft.Web/sites", "App Service", KnownMonikers.Web),
        ("Microsoft.Web/serverfarms", "App Service Plan", KnownMonikers.ApplicationGroup),
        ("Microsoft.Storage/storageAccounts", "Storage Account", KnownMonikers.AzureStorageAccount),
        ("Microsoft.KeyVault/vaults", "Key Vault", KnownMonikers.Key),
        ("Microsoft.Sql/servers", "SQL Server", KnownMonikers.Database),
        ("Microsoft.Compute/virtualMachines", "Virtual Machine", KnownMonikers.VirtualMachine),
        ("Microsoft.Cdn/profiles/endpoints", "Front Door", KnownMonikers.CloudGroup),
    ];

    /// <summary>
    /// Fast lookup for resource type metadata (cached for performance).
    /// </summary>
    private static readonly Dictionary<string, (string DisplayName, ImageMoniker Icon)> _resourceTypeLookup =
        _searchableResourceTypes.ToDictionary(
            rt => rt.ResourceType,
            rt => (rt.DisplayName, rt.Icon),
            StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Combined OData filter for all searchable resource types (single API call).
    /// </summary>
    private static readonly string _combinedResourceTypeFilter = string.Join(" or ",
        _searchableResourceTypes.Select(rt => $"resourceType eq '{rt.ResourceType}'"));

    /// <summary>
    /// Searches through already-loaded tree nodes for instant results (no API calls).
    /// This provides immediate feedback while the API search runs in the background.
    /// </summary>
    /// <param name="rootNodes">The current tree root nodes to search through.</param>
    /// <param name="searchText">The text to search for (case-insensitive substring match).</param>
    /// <param name="onResultFound">Callback invoked for each matching node found.</param>
    /// <param name="foundResourceIds">Set to track found resource IDs for deduplication with API results.</param>
    /// <returns>Number of local results found.</returns>
    public int SearchCachedNodes(
        IEnumerable<ExplorerNodeBase> rootNodes,
        string searchText,
        Action<SearchResultNode> onResultFound,
        ConcurrentDictionary<string, byte> foundResourceIds)
    {
        if (rootNodes == null || string.IsNullOrWhiteSpace(searchText))
            return 0;

        var count = 0;

        foreach (ExplorerNodeBase rootNode in rootNodes)
        {
            SearchNodeRecursive(rootNode, searchText, onResultFound, foundResourceIds, ref count, null, null);
        }

        return count;
    }

    private void SearchNodeRecursive(
        ExplorerNodeBase node,
        string searchText,
        Action<SearchResultNode> onResultFound,
        ConcurrentDictionary<string, byte> foundResourceIds,
        ref int count,
        string accountName,
        string subscriptionName)
    {
        if (node == null)
            return;

        // Track context as we descend the tree
        var currentAccountName = accountName;
        var currentSubscriptionName = subscriptionName;

        // Update context based on node type
        if (node is AccountNode acctNode)
        {
            currentAccountName = acctNode.Label;
        }
        else if (node is SubscriptionNode subNode)
        {
            currentSubscriptionName = subNode.Label;
        }

        // Check if this node is a searchable resource that matches
        if (node is IPortalResource resource &&
            !string.IsNullOrEmpty(resource.AzureResourceProvider) &&
            _resourceTypeLookup.TryGetValue(resource.AzureResourceProvider, out (string DisplayName, ImageMoniker Icon) metadata))
        {
            var resourceName = resource.ResourceName ?? node.Label;

            // Case-insensitive substring match on resource name
            if (resourceName != null && resourceName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Build resource ID for deduplication
                var resourceId = $"/subscriptions/{resource.SubscriptionId}/resourceGroups/{resource.ResourceGroupName}/providers/{resource.AzureResourceProvider}/{resourceName}";

                // Only add if not already found (deduplication)
                if (foundResourceIds.TryAdd(resourceId.ToLowerInvariant(), 0))
                {
                    var resultNode = new SearchResultNode(
                        resourceName,
                        metadata.DisplayName,
                        resourceId,
                        resource.SubscriptionId,
                        currentSubscriptionName ?? "Unknown",
                        currentAccountName ?? "Unknown",
                        metadata.Icon,
                        node); // Pass the actual node for expansion

                    onResultFound?.Invoke(resultNode);
                    count++;
                }
            }
        }

        // Only recurse into children if they're already loaded (don't trigger lazy loading)
        if (node.IsLoaded && node.Children != null)
        {
            foreach (ExplorerNodeBase child in node.Children.ToList()) // ToList to avoid collection modified
            {
                SearchNodeRecursive(child, searchText, onResultFound, foundResourceIds, ref count, currentAccountName, currentSubscriptionName);
            }
        }
    }

    /// <summary>
    /// Searches all Azure resources across all accounts, streaming SearchResultNode instances for tree display.
    /// Performs instant local search through cached nodes first, then continues with API search.
    /// </summary>
    /// <param name="searchText">The text to search for (case-insensitive substring match on resource name).</param>
    /// <param name="cachedRootNodes">Already-loaded tree nodes for instant local search (can be null).</param>
    /// <param name="onResultFound">Callback invoked on each result found (may be called from multiple threads).</param>
    /// <param name="onProgress">Callback for progress updates (subscriptionsSearched, totalSubscriptions).</param>
    /// <param name="cancellationToken">Token to cancel the search.</param>
    /// <returns>Total number of results found.</returns>
    public async Task<uint> SearchAllResourcesAsync(
        string searchText,
        IEnumerable<ExplorerNodeBase> cachedRootNodes,
        Action<SearchResultNode> onResultFound,
        Action<uint, uint> onProgress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return 0;

        // Track found resource IDs for deduplication between local and API results
        var foundResourceIds = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        var totalResults = 0;

        // Phase 1: Instant local search through cached nodes (no API calls)
        if (cachedRootNodes != null)
        {
            var localResults = SearchCachedNodes(cachedRootNodes, searchText, onResultFound, foundResourceIds);
            Interlocked.Add(ref totalResults, localResults);
        }

        // Phase 2: API search continues in background for additional results
        IReadOnlyList<AccountInfo> accounts = AzureAuthService.Instance.Accounts;
        if (accounts.Count == 0)
            return (uint)totalResults;

        var subscriptionsSearched = 0;
        var totalSubscriptionsEstimate = 0;

        // Higher concurrency for network-bound I/O operations
        var semaphore = new SemaphoreSlim(16);
        var searchTasks = new ConcurrentBag<Task>();

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
                                foundResourceIds,
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
        ConcurrentDictionary<string, byte> foundResourceIds,
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

            // Single API call with combined filter for all resource types (7x fewer API calls)
            await foreach (GenericResource resource in sub.GetGenericResourcesAsync(
                filter: _combinedResourceTypeFilter,
                cancellationToken: cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var resourceName = resource.Data.Name;

                // Case-insensitive substring match
                if (resourceName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                // Look up resource type metadata
                var resourceType = resource.Data.ResourceType.ToString();
                if (!_resourceTypeLookup.TryGetValue(resourceType, out (string DisplayName, ImageMoniker Icon) metadata))
                    continue;

                // Deduplicate: skip if already found in local search
                var resourceId = resource.Id.ToString();
                if (!foundResourceIds.TryAdd(resourceId.ToLowerInvariant(), 0))
                    continue;

                var resourceGroup = resource.Id.ResourceGroupName;

                // Create a lightweight resource node (properties load on expand)
                ExplorerNodeBase actualNode = CreateResourceNodeLightweight(
                    resourceType,
                    resourceName,
                    subInfo.SubscriptionId,
                    resourceGroup,
                    resource);

                var resultNode = new SearchResultNode(
                    resourceName,
                    metadata.DisplayName,
                    resourceId,
                    subInfo.SubscriptionId,
                    subInfo.SubscriptionName,
                    subInfo.AccountName,
                    metadata.Icon,
                    actualNode);

                onResultFound?.Invoke(resultNode);
            }
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

    private sealed class SubscriptionSearchInfo(string accountId, string accountName, string subscriptionId, string subscriptionName, string tenantId)
    {
        public string AccountId { get; } = accountId;
        public string AccountName { get; } = accountName;
        public string SubscriptionId { get; } = subscriptionId;
        public string SubscriptionName { get; } = subscriptionName;
        public string TenantId { get; } = tenantId;
    }
}
