using System;
using System.Threading;
using System.Threading.Tasks;

using AzureExplorer.AppService.Models;
using AzureExplorer.FrontDoor.Models;
using AzureExplorer.FunctionApp.Models;
using AzureExplorer.KeyVault.Models;
using AzureExplorer.ResourceGroup.Models;
using AzureExplorer.Sql.Models;
using AzureExplorer.Storage.Models;

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

            try
            {
                // Add subscription-level category nodes first
                // These allow users to see resources they have direct access to,
                // even without list permissions on the containing resource group
                var appServicesNode = new SubscriptionAppServicesNode(SubscriptionId);
                var functionAppsNode = new SubscriptionFunctionAppsNode(SubscriptionId);
                var frontDoorsNode = new SubscriptionFrontDoorsNode(SubscriptionId);
                var keyVaultsNode = new SubscriptionKeyVaultsNode(SubscriptionId);
                var storageAccountsNode = new SubscriptionStorageAccountsNode(SubscriptionId);
                var sqlServersNode = new SubscriptionSqlServersNode(SubscriptionId);

                AddChild(appServicesNode);
                AddChild(functionAppsNode);
                AddChild(frontDoorsNode);
                AddChild(keyVaultsNode);
                AddChild(sqlServersNode);
                AddChild(storageAccountsNode);

                // Add resource groups under a parent node
                var resourceGroupsNode = new ResourceGroupsNode(SubscriptionId);
                AddChild(resourceGroupsNode);

                // Pre-load subscription-level categories and resource groups in parallel
                _ = PreloadChildrenAsync(
                    appServicesNode, functionAppsNode, frontDoorsNode, 
                    keyVaultsNode, storageAccountsNode, sqlServersNode,
                    resourceGroupsNode, cancellationToken);
            }
            finally
            {
                EndLoading();
            }
        }

        private static async Task PreloadChildrenAsync(
            SubscriptionAppServicesNode appServicesNode,
            SubscriptionFunctionAppsNode functionAppsNode,
            SubscriptionFrontDoorsNode frontDoorsNode,
            SubscriptionKeyVaultsNode keyVaultsNode,
            SubscriptionStorageAccountsNode storageAccountsNode,
            SubscriptionSqlServersNode sqlServersNode,
            ResourceGroupsNode resourceGroupsNode,
            CancellationToken cancellationToken)
        {
            try
            {
                await Task.WhenAll(
                    appServicesNode.LoadChildrenAsync(cancellationToken),
                    functionAppsNode.LoadChildrenAsync(cancellationToken),
                    frontDoorsNode.LoadChildrenAsync(cancellationToken),
                    keyVaultsNode.LoadChildrenAsync(cancellationToken),
                    storageAccountsNode.LoadChildrenAsync(cancellationToken),
                    sqlServersNode.LoadChildrenAsync(cancellationToken),
                    resourceGroupsNode.LoadChildrenAsync(cancellationToken));
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
