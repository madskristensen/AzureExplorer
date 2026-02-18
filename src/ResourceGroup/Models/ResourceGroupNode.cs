using System.Threading;
using System.Threading.Tasks;

using AzureExplorer.AppService.Models;
using AzureExplorer.AppServicePlan.Models;
using AzureExplorer.Core.Models;
using AzureExplorer.FrontDoor.Models;
using AzureExplorer.KeyVault.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.ResourceGroup.Models
{
    /// <summary>
    /// Represents an Azure resource group. Contains category nodes for different
    /// resource types (App Services, App Service Plans, etc.).
    /// </summary>
    internal sealed class ResourceGroupNode : ExplorerNodeBase, IPortalResource
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

        public override ImageMoniker IconMoniker => KnownMonikers.AzureResourceGroup;
        public override int ContextMenuId => PackageIds.ResourceGroupContextMenu;
        public override bool SupportsChildren => true;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            // Add category nodes for each resource type
            var appServicesNode = new AppServicesNode(SubscriptionId, ResourceGroupName);
            var appServicePlansNode = new AppServicePlansNode(SubscriptionId, ResourceGroupName);
            var frontDoorsNode = new FrontDoorsNode(SubscriptionId, ResourceGroupName);
            var keyVaultsNode = new KeyVaultsNode(SubscriptionId, ResourceGroupName);

            AddChild(appServicesNode);
            AddChild(appServicePlansNode);
            AddChild(frontDoorsNode);
            AddChild(keyVaultsNode);

            EndLoading();

            // Pre-load category children in parallel (fire-and-forget with error handling)
            _ = PreloadCategoryChildrenAsync(appServicesNode, appServicePlansNode, frontDoorsNode, keyVaultsNode, cancellationToken);
        }

        private static async Task PreloadCategoryChildrenAsync(
            AppServicesNode appServicesNode,
            AppServicePlansNode appServicePlansNode,
            FrontDoorsNode frontDoorsNode,
            KeyVaultsNode keyVaultsNode,
            CancellationToken cancellationToken)
        {
            try
            {
                await Task.WhenAll(
                    appServicesNode.LoadChildrenAsync(cancellationToken),
                    appServicePlansNode.LoadChildrenAsync(cancellationToken),
                    frontDoorsNode.LoadChildrenAsync(cancellationToken),
                    keyVaultsNode.LoadChildrenAsync(cancellationToken));
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
