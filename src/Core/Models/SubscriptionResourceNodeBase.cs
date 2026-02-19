using System;
using System.Threading;
using System.Threading.Tasks;

using Azure.ResourceManager;
using Azure.ResourceManager.Resources;

using AzureExplorer.Core.Services;

namespace AzureExplorer.Core.Models
{
    /// <summary>
    /// Base class for subscription-level resource category nodes that query resources
    /// across the entire subscription using the generic resources API.
    /// </summary>
    internal abstract class SubscriptionResourceNodeBase : ExplorerNodeBase
    {
        protected SubscriptionResourceNodeBase(string label, string subscriptionId)
            : base(label)
        {
            SubscriptionId = subscriptionId;
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }

        /// <summary>
        /// The Azure resource type to filter (e.g., 'Microsoft.KeyVault/vaults').
        /// </summary>
        protected abstract string ResourceType { get; }

        public override bool SupportsChildren => true;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            try
            {
                ArmClient client = AzureResourceService.Instance.GetClient(SubscriptionId);
                SubscriptionResource sub = client.GetSubscriptionResource(
                    SubscriptionResource.CreateResourceIdentifier(SubscriptionId));

                var count = 0;

                // Filter by resource type using OData filter
                var filter = $"resourceType eq '{ResourceType}'";

                // Expand to include resource properties (state, etc.)
                await foreach (GenericResource resource in sub.GetGenericResourcesAsync(filter: filter, expand: "properties", cancellationToken: cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Allow derived classes to filter resources (e.g., by kind)
                    if (!ShouldIncludeResource(resource))
                        continue;

                    var name = resource.Data.Name;
                    var resourceGroup = resource.Id.ResourceGroupName;

                    ExplorerNodeBase node = CreateNodeFromResource(name, resourceGroup, resource);
                    if (node != null)
                    {
                        InsertChildSorted(node);
                        count++;
                        Description = $"loading... ({count} found)";
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Subscription query for {ResourceType}: found {count} resources");
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                Children.Clear();
                Children.Add(new LoadingNode { Label = $"Error: {ex.Message}" });
            }
            finally
            {
                EndLoading();
            }
        }

        /// <summary>
        /// Determines whether a resource should be included in the results.
        /// Override to filter resources beyond the resource type (e.g., by kind).
        /// </summary>
        protected virtual bool ShouldIncludeResource(GenericResource resource) => true;

        /// <summary>
        /// Creates a child node from the generic resource.
        /// </summary>
        protected abstract ExplorerNodeBase CreateNodeFromResource(string name, string resourceGroup, GenericResource resource);

        /// <summary>
        /// Inserts a child node in alphabetically sorted order by label.
        /// </summary>
        protected void InsertChildSorted(ExplorerNodeBase node)
        {
            node.Parent = this;

            var index = 0;
            while (index < Children.Count &&
                   string.Compare(Children[index].Label, node.Label, StringComparison.OrdinalIgnoreCase) < 0)
            {
                index++;
            }

            Children.Insert(index, node);
        }
    }
}
