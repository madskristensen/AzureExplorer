using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azure.ResourceManager.Resources;

using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.ResourceGroup.Models
{
    /// <summary>
    /// Category node that groups all resource groups under a subscription.
    /// </summary>
    internal sealed class ResourceGroupsNode : ExplorerNodeBase
    {
        private const string ResourceProvider = "Microsoft.Resources/resourceGroups";

        public ResourceGroupsNode(string subscriptionId)
            : base("Resource Groups")
        {
            SubscriptionId = subscriptionId;
            Children.Add(new LoadingNode());

            // Subscribe to resource events to sync across views
            ResourceNotificationService.ResourceCreated += OnResourceCreated;
            ResourceNotificationService.ResourceDeleted += OnResourceDeleted;
        }

        public string SubscriptionId { get; }

        public override ImageMoniker IconMoniker => KnownMonikers.AzureResourceGroup;
        public override int ContextMenuId => PackageIds.ResourceGroupsCategoryContextMenu;
        public override bool SupportsChildren => true;

        private void OnResourceCreated(object sender, ResourceCreatedEventArgs e)
        {
            if (!ShouldHandleEvent(e.ResourceType, e.SubscriptionId))
                return;

            if (!IsLoaded)
                return;

            // Check if already exists
            foreach (var child in Children)
            {
                if (child is ResourceGroupNode existing &&
                    string.Equals(existing.Label, e.ResourceName, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            // Add the new resource group
            var newNode = new ResourceGroupNode(e.ResourceName, SubscriptionId);
            InsertChildSorted(newNode);
        }

        private void OnResourceDeleted(object sender, ResourceDeletedEventArgs e)
        {
            if (!ShouldHandleEvent(e.ResourceType, e.SubscriptionId))
                return;

            // Find and remove the matching child node
            for (int i = Children.Count - 1; i >= 0; i--)
            {
                if (Children[i] is ResourceGroupNode node &&
                    string.Equals(node.Label, e.ResourceName, StringComparison.OrdinalIgnoreCase))
                {
                    Children.RemoveAt(i);
                    break;
                }
            }
        }

        private bool ShouldHandleEvent(string resourceType, string subscriptionId)
        {
            return string.Equals(resourceType, ResourceProvider, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(subscriptionId, SubscriptionId, StringComparison.OrdinalIgnoreCase);
        }

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            try
            {
                var resourceGroups = new List<ResourceGroupNode>();

                await foreach (ResourceGroupResource rg in AzureResourceService.Instance.GetResourceGroupsAsync(SubscriptionId, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    resourceGroups.Add(new ResourceGroupNode(rg.Data.Name, SubscriptionId));
                }

                // Sort alphabetically by name
                foreach (ResourceGroupNode node in resourceGroups.OrderBy(r => r.Label, StringComparer.OrdinalIgnoreCase))
                {
                    AddChild(node);
                }
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
        /// Adds a new resource group node in sorted order without refreshing existing nodes.
        /// </summary>
        /// <returns>The newly created node.</returns>
        public ResourceGroupNode AddResourceGroup(string name)
        {
            var newNode = new ResourceGroupNode(name, SubscriptionId);
            InsertChildSorted(newNode);
            return newNode;
        }
    }
}
