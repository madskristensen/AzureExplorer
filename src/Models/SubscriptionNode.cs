using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azure.ResourceManager.Resources;

using AzureExplorer.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Models
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

                // Then add resource groups
                var resourceGroups = new List<ResourceGroupNode>();

                await foreach (ResourceGroupResource rg in AzureResourceService.Instance.GetResourceGroupsAsync(SubscriptionId, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    resourceGroups.Add(new ResourceGroupNode(rg.Data.Name, SubscriptionId));
                }

                // Sort alphabetically by name
                foreach (var node in resourceGroups.OrderBy(r => r.Label, StringComparer.OrdinalIgnoreCase))
                {
                    AddChild(node);
                }

                // Pre-load subscription-level categories in parallel
                _ = PreloadSubscriptionCategoriesAsync(appServicesNode, frontDoorsNode, keyVaultsNode, cancellationToken);
            }
            finally
            {
                EndLoading();
            }
        }

        private static async Task PreloadSubscriptionCategoriesAsync(
            SubscriptionAppServicesNode appServicesNode,
            SubscriptionFrontDoorsNode frontDoorsNode,
            SubscriptionKeyVaultsNode keyVaultsNode,
            CancellationToken cancellationToken)
        {
            try
            {
                await Task.WhenAll(
                    appServicesNode.LoadChildrenAsync(cancellationToken),
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
