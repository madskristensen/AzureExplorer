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
        private bool _isLoaded;

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

        /// <summary>
        /// Gets the Azure resource type for external batched queries.
        /// </summary>
        public string GetResourceType() => ResourceType;

        /// <summary>
        /// Gets whether this node has resources (children loaded and count > 0).
        /// </summary>
        public bool HasResources => _isLoaded && Children.Count > 0;

        /// <summary>
        /// Gets whether this node should be visible in the tree view.
        /// Nodes are hidden until loaded. Empty nodes are only visible when ShowAll is enabled.
        /// </summary>
        public override bool IsVisible => HasResources || Options.GeneralOptions.Instance.ShowAll;

        /// <summary>
        /// Gets the opacity for this node. Returns 1.0 if has resources, 0.5 (dimmed) otherwise.
        /// Unloaded and empty nodes appear dimmed when ShowAll is enabled.
        /// </summary>
        public override double Opacity => HasResources ? 1.0 : 0.5;

        /// <summary>
        /// Notifies the UI that the visibility and opacity properties have changed.
        /// Call this after the ShowAll setting changes.
        /// </summary>
        public void NotifyVisibilityChanged()
        {
            OnPropertyChanged(nameof(IsVisible));
            OnPropertyChanged(nameof(Opacity));
        }

        public override bool SupportsChildren => true;

        /// <summary>
        /// Populates children from pre-fetched Resource Graph results.
        /// Used for batched loading where all resource types are queried at once.
        /// </summary>
        public void PopulateFromBatchedResults(IEnumerable<ResourceGraphResult> resources, CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            try
            {
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
            finally
            {
                _isLoaded = true;
                EndLoading();
                OnPropertyChanged(nameof(IsVisible));
                OnPropertyChanged(nameof(Opacity));
            }
        }

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
                _isLoaded = true;
                EndLoading();
                OnPropertyChanged(nameof(IsVisible));
                OnPropertyChanged(nameof(Opacity));
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
    }
}
