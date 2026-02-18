using System;
using System.Threading;
using System.Threading.Tasks;

using AzureExplorer.AppService.Models;
using AzureExplorer.FrontDoor.Models;
using AzureExplorer.KeyVault.Models;
using AzureExplorer.ResourceGroup.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Core.Models
{
    internal sealed class SubscriptionNode : ExplorerNodeBase
    {
        public SubscriptionNode(string name, string subscriptionId) : base(name)
        {
            SubscriptionId = subscriptionId;
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }

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
                var frontDoorsNode = new SubscriptionFrontDoorsNode(SubscriptionId);
                var keyVaultsNode = new SubscriptionKeyVaultsNode(SubscriptionId);

                AddChild(appServicesNode);
                AddChild(frontDoorsNode);
                AddChild(keyVaultsNode);

                // Add resource groups under a parent node
                var resourceGroupsNode = new ResourceGroupsNode(SubscriptionId);
                AddChild(resourceGroupsNode);

                // Pre-load subscription-level categories and resource groups in parallel
                _ = PreloadChildrenAsync(appServicesNode, frontDoorsNode, keyVaultsNode, resourceGroupsNode, cancellationToken);
            }
            finally
            {
                EndLoading();
            }
        }

        private static async Task PreloadChildrenAsync(
            SubscriptionAppServicesNode appServicesNode,
            SubscriptionFrontDoorsNode frontDoorsNode,
            SubscriptionKeyVaultsNode keyVaultsNode,
            ResourceGroupsNode resourceGroupsNode,
            CancellationToken cancellationToken)
        {
            try
            {
                await Task.WhenAll(
                    appServicesNode.LoadChildrenAsync(cancellationToken),
                    frontDoorsNode.LoadChildrenAsync(cancellationToken),
                    keyVaultsNode.LoadChildrenAsync(cancellationToken),
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
