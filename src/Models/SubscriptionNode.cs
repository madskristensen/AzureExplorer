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
            }
            finally
            {
                EndLoading();
            }
        }
    }
}
