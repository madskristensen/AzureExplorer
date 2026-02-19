using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.ResourceGraph;
using Azure.ResourceManager.ResourceGraph.Models;
using Azure.ResourceManager.Resources;

namespace AzureExplorer.Core.Services
{
    /// <summary>
    /// Service for querying Azure resources using Azure Resource Graph.
    /// Provides fast, server-side filtering across all subscriptions.
    /// </summary>
    internal sealed class ResourceGraphService
    {
        private static readonly Lazy<ResourceGraphService> _instance = new(() => new ResourceGraphService());

        private ResourceGraphService() { }

        public static ResourceGraphService Instance => _instance.Value;

        /// <summary>
        /// Queries resources for a specific subscription using Azure Resource Graph.
        /// </summary>
        /// <param name="subscriptionId">The subscription to query.</param>
        /// <param name="resourceTypes">Resource types to include (e.g., "microsoft.web/sites").</param>
        /// <param name="resourceGroup">Optional resource group filter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of resources with their properties.</returns>
        public async Task<IReadOnlyList<ResourceGraphResult>> QueryResourcesAsync(
            string subscriptionId,
            IEnumerable<string> resourceTypes,
            string resourceGroup = null,
            CancellationToken cancellationToken = default)
        {
            var results = new List<ResourceGraphResult>();

            // Get the subscription to find its actual tenant
            ArmClient client = AzureResourceService.Instance.GetClient(subscriptionId);
            SubscriptionResource subscriptionResource = client.GetSubscriptionResource(
                SubscriptionResource.CreateResourceIdentifier(subscriptionId));

            // Get the subscription data to find the owning tenant
            Response<SubscriptionResource> subResponse = await subscriptionResource.GetAsync(cancellationToken).ConfigureAwait(false);
            SubscriptionData subData = subResponse.Value.Data;

            var actualTenantId = subData.TenantId?.ToString();
            if (string.IsNullOrEmpty(actualTenantId))
                return results;

            // Get context to find the account
            (string AccountId, string TenantId)? context = AzureResourceService.Instance.GetSubscriptionContext(subscriptionId);
            var accountId = context?.AccountId;
            if (string.IsNullOrEmpty(accountId))
                return results;

            // Create a client scoped to the subscription's actual tenant
            ArmClient tenantClient = AzureResourceService.Instance.GetSilentClient(accountId, actualTenantId);

            // Get the tenant resource
            TenantResource tenantResource = null;
            await foreach (TenantResource tenant in tenantClient.GetTenants().GetAllAsync(cancellationToken).ConfigureAwait(false))
            {
                tenantResource = tenant;
                break;
            }

            if (tenantResource == null)
                return results;

            // Build Kusto query
            var kustoQuery = BuildQuery(subscriptionId, resourceTypes, resourceGroup);

            // Create query content with subscription scope
            var queryContent = new ResourceQueryContent(kustoQuery);
            queryContent.Subscriptions.Add(subscriptionId);

            try
            {
                // Execute query
                Response<ResourceQueryResult> response = await tenantResource.GetResourcesAsync(queryContent, cancellationToken).ConfigureAwait(false);
                ResourceQueryResult result = response.Value;

                // Parse results
                using var dataDoc = JsonDocument.Parse(result.Data.ToString());
                JsonElement dataElement = dataDoc.RootElement;

                if (dataElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement item in dataElement.EnumerateArray())
                    {
                        ResourceGraphResult resourceResult = ParseResourceResult(item);
                        if (resourceResult != null)
                        {
                            results.Add(resourceResult);
                        }
                    }
                }
            }
            catch
            {
                // Return empty results so caller can fall back to ARM API
            }

            return results;
        }

        /// <summary>
        /// Queries resources by type for a subscription, returning all resources of that type.
        /// </summary>
        public async Task<IReadOnlyList<ResourceGraphResult>> QueryByTypeAsync(
            string subscriptionId,
            string resourceType,
            string resourceGroup = null,
            CancellationToken cancellationToken = default)
        {
            return await QueryResourcesAsync(
                subscriptionId,
                [resourceType],
                resourceGroup,
                cancellationToken);
        }

        private static string BuildQuery(string subscriptionId, IEnumerable<string> resourceTypes, string resourceGroup)
        {
            var types = string.Join("', '", resourceTypes).ToLowerInvariant();
            var query = $"Resources | where subscriptionId == '{subscriptionId}' | where type in~ ('{types}')";

            if (!string.IsNullOrEmpty(resourceGroup))
            {
                query += $" | where resourceGroup =~ '{resourceGroup}'";
            }

            query += " | project id, name, type, subscriptionId, resourceGroup, location, tags, kind, sku, properties";
            query += " | order by name asc";

            return query;
        }

        private static ResourceGraphResult ParseResourceResult(JsonElement item)
        {
            try
            {
                var result = new ResourceGraphResult
                {
                    Id = item.GetProperty("id").GetString(),
                    Name = item.GetProperty("name").GetString(),
                    Type = item.GetProperty("type").GetString(),
                    SubscriptionId = item.GetProperty("subscriptionId").GetString(),
                    ResourceGroup = item.GetProperty("resourceGroup").GetString(),
                };

                if (item.TryGetProperty("location", out JsonElement locationElement))
                    result.Location = locationElement.GetString();

                if (item.TryGetProperty("kind", out JsonElement kindElement) && kindElement.ValueKind == JsonValueKind.String)
                    result.Kind = kindElement.GetString();

                if (item.TryGetProperty("tags", out JsonElement tagsElement) && tagsElement.ValueKind == JsonValueKind.Object)
                {
                    result.Tags = new Dictionary<string, string>();
                    foreach (JsonProperty tag in tagsElement.EnumerateObject())
                    {
                        if (tag.Value.ValueKind == JsonValueKind.String)
                        {
                            result.Tags[tag.Name] = tag.Value.GetString();
                        }
                    }
                }

                if (item.TryGetProperty("sku", out JsonElement skuElement) && skuElement.ValueKind == JsonValueKind.Object)
                {
                    if (skuElement.TryGetProperty("name", out JsonElement skuNameElement))
                        result.SkuName = skuNameElement.GetString();
                    if (skuElement.TryGetProperty("tier", out JsonElement skuTierElement))
                        result.SkuTier = skuTierElement.GetString();
                }

                if (item.TryGetProperty("properties", out JsonElement propsElement) && propsElement.ValueKind == JsonValueKind.Object)
                {
                    result.Properties = propsElement.Clone();
                }

                return result;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Represents a resource returned from Azure Resource Graph.
    /// </summary>
    internal sealed class ResourceGraphResult
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string SubscriptionId { get; set; }
        public string ResourceGroup { get; set; }
        public string Location { get; set; }
        public string Kind { get; set; }
        public string SkuName { get; set; }
        public string SkuTier { get; set; }
        public IDictionary<string, string> Tags { get; set; }
        public JsonElement? Properties { get; set; }

        /// <summary>
        /// Gets a property value from the properties object.
        /// </summary>
        public string GetProperty(string path)
        {
            if (!Properties.HasValue)
                return null;

            try
            {
                JsonElement current = Properties.Value;
                var parts = path.Split('.');

                foreach (var part in parts)
                {
                    if (current.TryGetProperty(part, out JsonElement next))
                        current = next;
                    else
                        return null;
                }

                return current.ValueKind == JsonValueKind.String ? current.GetString() : current.ToString();
            }
            catch
            {
                return null;
            }
        }
    }
}
