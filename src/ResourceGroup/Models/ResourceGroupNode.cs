using System.Threading;

using AzureExplorer.AppService.Models;
using AzureExplorer.AppServicePlan.Models;
using AzureExplorer.Core.Models;
using AzureExplorer.Core.Options;
using AzureExplorer.Core.Services;
using AzureExplorer.FrontDoor.Models;
using AzureExplorer.FunctionApp.Models;
using AzureExplorer.KeyVault.Models;
using AzureExplorer.Sql.Models;
using AzureExplorer.Storage.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.ResourceGroup.Models
{
    /// <summary>
    /// Represents an Azure resource group. Contains category nodes for different
    /// resource types (App Services, App Service Plans, etc.).
    /// </summary>
    internal sealed class ResourceGroupNode : ExplorerNodeBase, IPortalResource, IDeletableResource
    {
        public ResourceGroupNode(string name, string subscriptionId) : base(name)
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = name;
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }

        // IPortalResource - resource groups don't have a provider path
        string IPortalResource.ResourceName => null;
        string IPortalResource.AzureResourceProvider => null;

        // IDeletableResource
        string IDeletableResource.DeleteResourceType => "Resource Group";
        string IDeletableResource.DeleteResourceName => ResourceGroupName;

        public override ImageMoniker IconMoniker => KnownMonikers.AzureResourceGroup;
        public override int ContextMenuId => PackageIds.ResourceGroupContextMenu;
        public override bool SupportsChildren => true;

        /// <summary>
        /// Deletes this resource group. Only succeeds if the resource group is empty.
        /// </summary>
        async Task IDeletableResource.DeleteAsync()
        {
            await AzureResourceService.Instance.DeleteResourceGroupAsync(SubscriptionId, ResourceGroupName);
        }

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            // Create category nodes for each resource type
            var appServicesNode = new AppServicesNode(SubscriptionId, ResourceGroupName);
            var appServicePlansNode = new AppServicePlansNode(SubscriptionId, ResourceGroupName);
            var functionAppsNode = new FunctionAppsNode(SubscriptionId, ResourceGroupName);
            var frontDoorsNode = new FrontDoorsNode(SubscriptionId, ResourceGroupName);
            var keyVaultsNode = new KeyVaultsNode(SubscriptionId, ResourceGroupName);
            var sqlServersNode = new SqlServersNode(SubscriptionId, ResourceGroupName);
            var storageAccountsNode = new StorageAccountsNode(SubscriptionId, ResourceGroupName);

            ExplorerNodeBase[] resourceTypeNodes =
            [
                appServicesNode, appServicePlansNode, frontDoorsNode,
                functionAppsNode, keyVaultsNode, sqlServersNode, storageAccountsNode
            ];

            if (!GeneralOptions.Instance.ShowAll)
            {
                // Keep loading indicator visible - EndLoading will be called by LoadAndAddNonEmptyNodesAsync
                _ = LoadAndAddNonEmptyNodesAsync(this, resourceTypeNodes, cancellationToken);
            }
            else
            {
                // Add all nodes immediately, then load children in background
                foreach (ExplorerNodeBase node in resourceTypeNodes)
                {
                    AddChild(node);
                }

                EndLoading();

                _ = PreloadCategoryChildrenAsync(resourceTypeNodes, cancellationToken);
            }
        }

        private static async Task LoadAndAddNonEmptyNodesAsync(
            ResourceGroupNode parent,
            ExplorerNodeBase[] resourceTypeNodes,
            CancellationToken cancellationToken)
        {
            try
            {
                // Load all resource type nodes in parallel, adding each as it completes
                var loadTasks = new Task[resourceTypeNodes.Length];

                for (var i = 0; i < resourceTypeNodes.Length; i++)
                {
                    ExplorerNodeBase node = resourceTypeNodes[i];
                    loadTasks[i] = LoadAndAddIfNotEmptyAsync(parent, node, cancellationToken);
                }

                await Task.WhenAll(loadTasks);
            }
            catch (OperationCanceledException)
            {
                // Expected when user navigates away; ignore
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load failed: {ex.Message}");
            }
            finally
            {
                // End loading state now that we've added the nodes
                await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                parent.EndLoading();
            }
        }

        private static async Task LoadAndAddIfNotEmptyAsync(
            ResourceGroupNode parent,
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

        private static async Task PreloadCategoryChildrenAsync(
            ExplorerNodeBase[] resourceTypeNodes,
            CancellationToken cancellationToken)
        {
            try
            {
                var loadTasks = new Task[resourceTypeNodes.Length];
                for (var i = 0; i < resourceTypeNodes.Length; i++)
                {
                    loadTasks[i] = resourceTypeNodes[i].LoadChildrenAsync(cancellationToken);
                }

                await Task.WhenAll(loadTasks);
            }
            catch (OperationCanceledException)
            {
                // Expected when user navigates away; ignore
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Pre-load failed: {ex.Message}");
            }
        }
    }
}
