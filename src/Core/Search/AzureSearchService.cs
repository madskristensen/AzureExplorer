using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.ResourceGraph;
using Azure.ResourceManager.ResourceGraph.Models;
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
/// Parsed search query with optional tag filter.
/// Supports syntax: "searchterm" or "tag:Key=Value" or "searchterm tag:Key=Value"
/// </summary>
internal sealed class ParsedSearchQuery
{
    private static readonly Regex _tagPattern = new(@"tag:([^=\s]+)(?:=([^\s]+))?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string TextQuery { get; private set; }
    public string TagKey { get; private set; }
    public string TagValue { get; private set; }
    public bool HasTagFilter => !string.IsNullOrEmpty(TagKey);

    public static ParsedSearchQuery Parse(string searchText)
    {
        var result = new ParsedSearchQuery();

        if (string.IsNullOrWhiteSpace(searchText))
            return result;

        // Extract tag filter if present
        Match match = _tagPattern.Match(searchText);
        if (match.Success)
        {
            result.TagKey = match.Groups[1].Value;
            result.TagValue = match.Groups[2].Success ? match.Groups[2].Value : null;

            // Remove tag filter from text query
            result.TextQuery = _tagPattern.Replace(searchText, "").Trim();
        }
        else
        {
            result.TextQuery = searchText.Trim();
        }

        return result;
    }

    /// <summary>
    /// Checks if a node matches this query (text match and/or tag filter).
    /// </summary>
    public bool Matches(string resourceName, ITaggableResource taggable)
    {
        // Check tag filter first (if specified)
        if (HasTagFilter)
        {
            if (taggable == null || !taggable.HasTag(TagKey, TagValue))
                return false;
        }

        // Check text query (if specified)
        if (!string.IsNullOrEmpty(TextQuery))
        {
            if (string.IsNullOrEmpty(resourceName))
                return false;

            if (resourceName.IndexOf(TextQuery, StringComparison.OrdinalIgnoreCase) < 0)
                return false;
        }

        // Must have matched at least one criteria
        return HasTagFilter || !string.IsNullOrEmpty(TextQuery);
    }
}

/// <summary>
/// Service for searching Azure resources across all logged-in accounts in parallel.
/// Results are streamed as SearchResultNode instances for tree view display.
/// 
/// Supports search syntax:
/// - "myapp" - search by name
/// - "tag:Environment=Production" - search by tag
/// - "myapp tag:Team=Backend" - combine name and tag filters
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
    /// Maximum number of search results to return to prevent memory pressure.
    /// </summary>
    private const int _maxSearchResults = 500;

    /// <summary>
    /// Searches through already-loaded tree nodes for instant results (no API calls).
    /// This provides immediate feedback while the API search runs in the background.
    /// </summary>
    /// <param name="rootNodes">The current tree root nodes to search through.</param>
    /// <param name="searchText">The text to search for (supports tag:Key=Value syntax).</param>
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

        var query = ParsedSearchQuery.Parse(searchText);
        var count = 0;

        foreach (ExplorerNodeBase rootNode in rootNodes)
        {
            SearchNodeRecursive(rootNode, query, onResultFound, foundResourceIds, ref count, null, null);
        }

        return count;
    }

    private void SearchNodeRecursive(
        ExplorerNodeBase node,
        ParsedSearchQuery query,
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

            // Check if node matches query (name and/or tag filter)
            var taggable = node as ITaggableResource;
            if (query.Matches(resourceName, taggable))
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
                SearchNodeRecursive(child, query, onResultFound, foundResourceIds, ref count, currentAccountName, currentSubscriptionName);
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

        var query = ParsedSearchQuery.Parse(searchText);

        // Track found resource IDs for deduplication between local and API results
        var foundResourceIds = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        var totalResults = 0;

        // Phase 1: Instant local search through cached nodes (no API calls)
        if (cachedRootNodes != null)
        {
            var localResults = SearchCachedNodes(cachedRootNodes, searchText, onResultFound, foundResourceIds);
            Interlocked.Add(ref totalResults, localResults);
        }

        // Phase 2: Use Azure Resource Graph for fast server-side search
        IReadOnlyList<AccountInfo> accounts = AzureAuthService.Instance.Accounts;
        if (accounts.Count == 0)
            return (uint)totalResults;

        var graphResults = await SearchWithResourceGraphAsync(
            accounts, query, foundResourceIds, onResultFound, onProgress, cancellationToken);
        Interlocked.Add(ref totalResults, (int)graphResults);

        return (uint)totalResults;
    }

    /// <summary>
    /// Uses Azure Resource Graph to search for resources (server-side filtering - much faster).
    /// Searches all subscriptions across all accounts in parallel.
    /// </summary>
    private async Task<uint> SearchWithResourceGraphAsync(
        IReadOnlyList<AccountInfo> accounts,
        ParsedSearchQuery query,
        ConcurrentDictionary<string, byte> foundResourceIds,
        Action<SearchResultNode> onResultFound,
        Action<uint, uint> onProgress,
        CancellationToken cancellationToken)
    {
        var totalResults = 0;
        var accountsSearched = 0;

        // Search each account in parallel
        IEnumerable<Task> accountTasks = accounts.Select(async account =>
        {
            try
            {
                // Get tenants for this account
                var tenants = new List<TenantInfo>();
                await foreach (TenantInfo tenant in AzureResourceService.Instance.GetTenantsForSearchAsync(account.AccountId, cancellationToken))
                {
                    tenants.Add(tenant);
                }

                // Search tenants in parallel for better performance (limit concurrency to avoid throttling)
                IEnumerable<Task> tenantTasks = tenants.Select(async tenant =>
                {
                    // Check if we've hit the result limit
                    if (totalResults >= _maxSearchResults)
                        return;

                    try
                    {
                        ArmClient client = AzureResourceService.Instance.GetSilentClient(account.AccountId, tenant.TenantId);
                        TenantResource tenantResource = client.GetTenants().FirstOrDefault();
                        if (tenantResource == null)
                            return;

                        // Build Kusto query for Resource Graph
                        var kustoQuery = BuildResourceGraphQuery(query);

                        var queryContent = new ResourceQueryContent(kustoQuery);

                        // Execute Resource Graph query
                        Response<ResourceQueryResult> response = await tenantResource.GetResourcesAsync(queryContent, cancellationToken);
                        ResourceQueryResult result = response.Value;

                        // Parse the response data as JSON
                        using var dataDoc = JsonDocument.Parse(result.Data.ToString());
                        JsonElement dataElement = dataDoc.RootElement;

                        if (dataElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (JsonElement item in dataElement.EnumerateArray())
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                // Check result limit to prevent memory pressure
                                if (totalResults >= _maxSearchResults)
                                    break;

                                var resourceId = item.GetProperty("id").GetString();
                                var name = item.GetProperty("name").GetString();
                                var type = item.GetProperty("type").GetString();
                                var subscriptionId = item.GetProperty("subscriptionId").GetString();
                                var resourceGroup = item.GetProperty("resourceGroup").GetString();

                                // Deduplicate
                                if (!foundResourceIds.TryAdd(resourceId.ToLowerInvariant(), 0))
                                    continue;

                                // Get metadata for this resource type
                                if (!_resourceTypeLookup.TryGetValue(type, out (string DisplayName, ImageMoniker Icon) metadata))
                                    continue;

                                // Parse tags from result
                                IDictionary<string, string> tags = null;
                                if (item.TryGetProperty("tags", out JsonElement tagsElement) && tagsElement.ValueKind == JsonValueKind.Object)
                                {
                                    tags = new Dictionary<string, string>();
                                    foreach (JsonProperty tag in tagsElement.EnumerateObject())
                                    {
                                        tags[tag.Name] = tag.Value.GetString();
                                    }
                                }

                                // Create node with tags
                                ExplorerNodeBase actualNode = CreateNodeFromResourceGraph(type, name, subscriptionId, resourceGroup, tags, item);

                                // Double-check name filter if specified (Resource Graph 'contains' is case-insensitive, but verify)
                                if (!string.IsNullOrEmpty(query.TextQuery) &&
                                    name.IndexOf(query.TextQuery, StringComparison.OrdinalIgnoreCase) < 0)
                                    continue;

                                var resultNode = new SearchResultNode(
                                    name,
                                    metadata.DisplayName,
                                    resourceId,
                                    subscriptionId,
                                    subscriptionId, // Use subscription ID as name (we don't have display name from graph)
                                    account.Username,
                                    metadata.Icon,
                                    actualNode);

                                Interlocked.Increment(ref totalResults);
                                onResultFound?.Invoke(resultNode);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Resource Graph search failed for tenant {tenant.TenantId}: {ex.Message}");
                    }
                });

                await Task.WhenAll(tenantTasks).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Resource Graph search failed for account {account.Username}: {ex.Message}");
            }
            finally
            {
                var searched = Interlocked.Increment(ref accountsSearched);
                onProgress?.Invoke((uint)searched, (uint)accounts.Count);
            }
        });

        await Task.WhenAll(accountTasks).ConfigureAwait(false);

        return (uint)totalResults;
    }

    /// <summary>
    /// Builds a Kusto query for Azure Resource Graph based on the search query.
    /// </summary>
    private static string BuildResourceGraphQuery(ParsedSearchQuery query)
    {
        // Start with resource types we support
        var resourceTypes = string.Join("', '", _searchableResourceTypes.Select(rt => rt.ResourceType.ToLowerInvariant()));
        var kustoQuery = $"Resources | where type in~ ('{resourceTypes}')";

        // Add tag filter
        // Azure Resource Graph tag keys are case-sensitive in the tags[] accessor,
        // so we try common case variations (lowercase, original, title case)
        if (query.HasTagFilter)
        {
            var keyLower = query.TagKey.ToLowerInvariant();
            var keyOriginal = query.TagKey;
            var keyTitle = char.ToUpperInvariant(query.TagKey[0]) + query.TagKey.Substring(1).ToLowerInvariant();

            if (!string.IsNullOrEmpty(query.TagValue))
            {
                // Filter by tag key and value (try common case variations for key, case-insensitive for value)
                kustoQuery += $" | where tags['{keyLower}'] =~ '{query.TagValue}' or tags['{keyOriginal}'] =~ '{query.TagValue}' or tags['{keyTitle}'] =~ '{query.TagValue}'";
            }
            else
            {
                // Filter by tag key only (any value) - try common case variations
                kustoQuery += $" | where isnotnull(tags['{keyLower}']) or isnotnull(tags['{keyOriginal}']) or isnotnull(tags['{keyTitle}'])";
            }
        }

        // Add name filter if specified (case-insensitive)
        // Escape single quotes to prevent KQL injection
        if (!string.IsNullOrEmpty(query.TextQuery))
        {
            var escapedText = query.TextQuery.Replace("'", "\\'");
            kustoQuery += $" | where name contains '{escapedText}'";
        }

        // Select fields we need
        kustoQuery += " | project id, name, type, subscriptionId, resourceGroup, tags, kind, sku";

        return kustoQuery;
    }

    /// <summary>
    /// Creates a resource node from Resource Graph query results.
    /// </summary>
    private static ExplorerNodeBase CreateNodeFromResourceGraph(
        string resourceType,
        string name,
        string subscriptionId,
        string resourceGroup,
        IDictionary<string, string> tags,
        JsonElement item)
    {
        try
        {
            string kind = null;
            if (item.TryGetProperty("kind", out JsonElement kindElement))
                kind = kindElement.GetString();

            return resourceType.ToLowerInvariant() switch
            {
                "microsoft.web/sites" => CreateWebSiteNodeFromGraph(name, subscriptionId, resourceGroup, kind, tags),
                "microsoft.web/serverfarms" => new AppServicePlanNode(name, subscriptionId, resourceGroup, null, kind, null),
                "microsoft.storage/storageaccounts" => new StorageAccountNode(name, subscriptionId, resourceGroup, null, kind, null, tags),
                "microsoft.keyvault/vaults" => new KeyVaultNode(name, subscriptionId, resourceGroup, null, null, tags),
                "microsoft.sql/servers" => new SqlServerNode(name, subscriptionId, resourceGroup, null, null, tags),
                "microsoft.compute/virtualmachines" => new VirtualMachineNode(name, subscriptionId, resourceGroup, null, null, null, null, null, tags),
                "microsoft.cdn/profiles/endpoints" => new FrontDoorNode(name, subscriptionId, resourceGroup, null, null),
                _ => null
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to create node from Resource Graph for {resourceType}/{name}: {ex.Message}");
            return null;
        }
    }

    private static ExplorerNodeBase CreateWebSiteNodeFromGraph(string name, string subscriptionId, string resourceGroup, string kind, IDictionary<string, string> tags)
    {
        if (!string.IsNullOrEmpty(kind) && kind.IndexOf("functionapp", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return new FunctionAppNode(name, subscriptionId, resourceGroup, null, null, tags);
        }
        return new AppServiceNode(name, subscriptionId, resourceGroup, null, null, tags);
    }
}
