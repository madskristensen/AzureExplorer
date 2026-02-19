using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Azure.ResourceManager;
using Azure.ResourceManager.Resources;

using AzureExplorer.Core.Services;

namespace AzureExplorer.Core.Models
{
    /// <summary>
    /// Base class for subscription-level resource category nodes that query resources
    /// across the entire subscription. Uses Azure Resource Graph for fast loading with
    /// fallback to ARM API when Resource Graph returns no results.
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
                // Try Azure Resource Graph first for fast loading
                IReadOnlyList<ResourceGraphResult> resources = await ResourceGraphService.Instance.QueryByTypeAsync(
                    SubscriptionId,
                    ResourceType,
                    resourceGroup: null,
                    cancellationToken);

                if (resources.Count > 0)
                {
                    // Resource Graph worked - use its results
                    var count = 0;
                    foreach (ResourceGraphResult resource in resources)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (!ShouldIncludeResource(resource))
                            continue;

                        ExplorerNodeBase node = CreateNodeFromGraphResult(resource);
                        if (node != null)
                        {
                            InsertChildSorted(node);
                            count++;
                            Description = $"loading... ({count} found)";
                        }
                    }
                }
                else
                {
                    // Resource Graph returned 0 results - fall back to ARM API
                    await LoadChildrenViaArmApiAsync(cancellationToken);
                }
            }
            catch
            {
                // Resource Graph failed - fall back to ARM API
                try
                {
                    await LoadChildrenViaArmApiAsync(cancellationToken);
                }
                catch (Exception armEx)
                {
                    await armEx.LogAsync();
                    Children.Clear();
                    Children.Add(new LoadingNode { Label = $"Error: {armEx.Message}" });
                }
            }
            finally
            {
                EndLoading();
            }
        }

        /// <summary>
        /// Fallback method that loads resources using the traditional ARM API.
        /// </summary>
        private async Task LoadChildrenViaArmApiAsync(CancellationToken cancellationToken)
        {
            ArmClient client = AzureResourceService.Instance.GetClient(SubscriptionId);
            SubscriptionResource sub = client.GetSubscriptionResource(
                SubscriptionResource.CreateResourceIdentifier(SubscriptionId));

            var count = 0;
            var filter = $"resourceType eq '{ResourceType}'";

            await foreach (GenericResource resource in sub.GetGenericResourcesAsync(filter: filter, expand: "properties", cancellationToken: cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!ShouldIncludeResourceArm(resource))
                    continue;

                ExplorerNodeBase node = CreateNodeFromArmResource(resource);
                if (node != null)
                {
                    InsertChildSorted(node);
                    count++;
                    Description = $"loading... ({count} found)";
                }
            }
        }

        /// <summary>
        /// Determines whether a resource should be included (Resource Graph version).
        /// </summary>
        protected virtual bool ShouldIncludeResource(ResourceGraphResult resource) => true;

        /// <summary>
        /// Determines whether a resource should be included (ARM API version).
        /// </summary>
        protected virtual bool ShouldIncludeResourceArm(GenericResource resource) => true;

        /// <summary>
        /// Creates a child node from the Resource Graph result.
        /// </summary>
        protected abstract ExplorerNodeBase CreateNodeFromGraphResult(ResourceGraphResult resource);

        /// <summary>
        /// Creates a child node from an ARM API GenericResource.
        /// Override this to provide ARM API fallback support.
        /// </summary>
        protected virtual ExplorerNodeBase CreateNodeFromArmResource(GenericResource resource)
        {
            // Default implementation - derived classes should override for full support
            return null;
        }

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
