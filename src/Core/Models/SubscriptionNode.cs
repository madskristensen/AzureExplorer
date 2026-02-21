using System.Collections.Generic;
using System.Linq;
using System.Threading;

using AzureExplorer.AppService.Models;
using AzureExplorer.Core.Options;
using AzureExplorer.Core.Services;
using AzureExplorer.FrontDoor.Models;
using AzureExplorer.FunctionApp.Models;
using AzureExplorer.KeyVault.Models;
using AzureExplorer.ResourceGroup.Models;
using AzureExplorer.Sql.Models;
using AzureExplorer.Storage.Models;
using AzureExplorer.VirtualMachine.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Core.Models
{
    internal sealed class SubscriptionNode : ExplorerNodeBase, IPortalResource
    {
        public SubscriptionNode(string name, string subscriptionId) : base(name)
        {
            SubscriptionId = subscriptionId;
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }

        /// <summary>
        /// Gets whether this subscription is marked as hidden in settings.
        /// Hidden subscriptions are only visible when ShowAll is enabled.
        /// </summary>
        public bool IsHidden => GeneralOptions.Instance.IsSubscriptionHidden(SubscriptionId);

        /// <summary>
        /// Gets whether this node should be visible in the tree view.
        /// Hidden subscriptions are only visible when ShowAll is enabled.
        /// </summary>
        public override bool IsVisible => !IsHidden || GeneralOptions.Instance.ShowAll;

        /// <summary>
        /// Gets the opacity for this node. Returns 0.5 (dimmed) if hidden, otherwise 1.0.
        /// </summary>
        public override double Opacity => IsHidden ? 0.5 : 1.0;

        /// <summary>
        /// Notifies the UI that the visibility and opacity properties have changed.
        /// Call this after toggling the hidden state or ShowAll setting.
        /// Also notifies child resource type nodes to update their visibility.
        /// </summary>
        public void NotifyVisibilityChanged()
        {
            OnPropertyChanged(nameof(IsVisible));
            OnPropertyChanged(nameof(Opacity));

            // Notify all child resource type nodes to update their visibility
            foreach (ExplorerNodeBase child in Children)
            {
                if (child is SubscriptionResourceNodeBase resourceNode)
                {
                    resourceNode.NotifyVisibilityChanged();
                }
            }
        }

        // IPortalResource - subscriptions don't have resource group or provider path
        string IPortalResource.ResourceGroupName => null;
        string IPortalResource.ResourceName => null;
        string IPortalResource.AzureResourceProvider => null;

        public override ImageMoniker IconMoniker => KnownMonikers.AzureSubscriptionKey;
        public override int ContextMenuId => PackageIds.SubscriptionContextMenu;
        public override bool SupportsChildren => true;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            // Create subscription-level category nodes
            var appServicesNode = new SubscriptionAppServicesNode(SubscriptionId);
            var functionAppsNode = new SubscriptionFunctionAppsNode(SubscriptionId);
            var frontDoorsNode = new SubscriptionFrontDoorsNode(SubscriptionId);
            var keyVaultsNode = new SubscriptionKeyVaultsNode(SubscriptionId);
            var storageAccountsNode = new SubscriptionStorageAccountsNode(SubscriptionId);
            var sqlServersNode = new SubscriptionSqlServersNode(SubscriptionId);
            var virtualMachinesNode = new SubscriptionVirtualMachinesNode(SubscriptionId);
            var resourceGroupsNode = new ResourceGroupsNode(SubscriptionId);

            ExplorerNodeBase[] resourceTypeNodes =
            [
                appServicesNode, functionAppsNode, frontDoorsNode,
                keyVaultsNode, sqlServersNode, storageAccountsNode, virtualMachinesNode
            ];

            // Always add all nodes - visibility is controlled by IsVisible property
            foreach (ExplorerNodeBase node in resourceTypeNodes)
            {
                AddChild(node);
            }
            AddChild(resourceGroupsNode);

            EndLoading();

            // Load children in background
            _ = PreloadChildrenAsync(resourceTypeNodes, resourceGroupsNode, cancellationToken);
        }

        private static async Task LoadAndAddNonEmptyNodesAsync(
            SubscriptionNode parent,
            ExplorerNodeBase[] resourceTypeNodes,
            ResourceGroupsNode resourceGroupsNode,
            CancellationToken cancellationToken)
        {
            try
            {
                // Collect unique resource types for the batched query
                // Note: Multiple nodes may share the same type (e.g., App Services and Function Apps both use Microsoft.Web/sites)
                var resourceTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var subscriptionResourceNodes = new List<SubscriptionResourceNodeBase>();

                foreach (ExplorerNodeBase node in resourceTypeNodes)
                {
                    if (node is SubscriptionResourceNodeBase resourceNode)
                    {
                        resourceTypes.Add(resourceNode.GetResourceType());
                        subscriptionResourceNodes.Add(resourceNode);
                    }
                }

                // Start loading resource groups in parallel (separate query)
                Task resourceGroupsTask = LoadAndAddNodeAsync(parent, resourceGroupsNode, cancellationToken);

                // Single batched query for ALL resource types
                IReadOnlyList<ResourceGraphResult> allResources = await ResourceGraphService.Instance.QueryResourcesAsync(
                    parent.SubscriptionId,
                    resourceTypes,
                    resourceGroup: null,
                    cancellationToken);

                // Group results by resource type
                ILookup<string, ResourceGraphResult> resourcesByType = allResources.ToLookup(
                    r => r.Type,
                    StringComparer.OrdinalIgnoreCase);

                // Populate each node from batched results and add to tree if non-empty
                // Each node uses ShouldIncludeResource to filter (e.g., Function Apps filter by kind)
                await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                foreach (SubscriptionResourceNodeBase node in subscriptionResourceNodes)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var resourceType = node.GetResourceType();
                    IEnumerable<ResourceGraphResult> nodeResources = resourcesByType[resourceType];
                    node.PopulateFromBatchedResults(nodeResources, cancellationToken);

                    if (node.Children.Count > 0)
                    {
                        parent.InsertChildSorted(node);
                    }
                }

                // Wait for resource groups to finish loading
                await resourceGroupsTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when user navigates away; ignore
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
            }
            finally
            {
                // End loading state now that we've added the nodes
                await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                parent.EndLoading();
            }
        }

        private static async Task LoadAndAddIfNotEmptyAsync(
            SubscriptionNode parent,
            ExplorerNodeBase node,
            CancellationToken cancellationToken)
        {
            var added = false;

            // Subscribe to Children changes to add the node as soon as the first child is discovered
            void OnChildrenChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            {
                if (!added && node.Children.Count > 0)
                {
                    added = true;
                    // Use BeginInvoke to add on UI thread without blocking the loading
                    Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                    {
                        await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        parent.InsertChildSorted(node);
                    }).FireAndForget();
                }
            }

            node.Children.CollectionChanged += OnChildrenChanged;

            try
            {
                await node.LoadChildrenAsync(cancellationToken);
            }
            finally
            {
                node.Children.CollectionChanged -= OnChildrenChanged;
            }
        }

        private static async Task LoadAndAddNodeAsync(
            SubscriptionNode parent,
            ExplorerNodeBase node,
            CancellationToken cancellationToken)
        {
            await node.LoadChildrenAsync(cancellationToken);
            await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Only add if it has children (respects HideEmptyResourceTypes setting)
            if (node.Children.Count > 0)
            {
                parent.InsertChildSorted(node);
            }
        }

        private static async Task PreloadChildrenAsync(
            ExplorerNodeBase[] resourceTypeNodes,
            ResourceGroupsNode resourceGroupsNode,
            CancellationToken cancellationToken)
        {
            try
            {
                var loadTasks = new Task[resourceTypeNodes.Length + 1];
                for (var i = 0; i < resourceTypeNodes.Length; i++)
                {
                    loadTasks[i] = resourceTypeNodes[i].LoadChildrenAsync(cancellationToken);
                }
                loadTasks[resourceTypeNodes.Length] = resourceGroupsNode.LoadChildrenAsync(cancellationToken);

                await Task.WhenAll(loadTasks);
            }
            catch (OperationCanceledException)
            {
                // Expected when user navigates away; ignore
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
            }
        }
    }
}
