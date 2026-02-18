using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Models
{
    /// <summary>
    /// Represents an Azure resource group. Contains category nodes for different
    /// resource types (App Services, App Service Plans, etc.).
    /// </summary>
    internal sealed class ResourceGroupNode : ExplorerNodeBase
    {
        public ResourceGroupNode(string name, string subscriptionId) : base(name)
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = name;
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }

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

            AddChild(appServicesNode);
            AddChild(appServicePlansNode);
            AddChild(frontDoorsNode);

            EndLoading();

            // Pre-load category children in parallel (fire-and-forget with error handling)
            _ = PreloadCategoryChildrenAsync(appServicesNode, appServicePlansNode, frontDoorsNode, cancellationToken);
        }

        private static async Task PreloadCategoryChildrenAsync(
            AppServicesNode appServicesNode,
            AppServicePlansNode appServicePlansNode,
            FrontDoorsNode frontDoorsNode,
            CancellationToken cancellationToken)
        {
            try
            {
                await Task.WhenAll(
                    appServicesNode.LoadChildrenAsync(cancellationToken),
                    appServicePlansNode.LoadChildrenAsync(cancellationToken),
                    frontDoorsNode.LoadChildrenAsync(cancellationToken));
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
