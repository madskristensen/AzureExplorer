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

        public override async Task LoadChildrenAsync()
        {
            if (!BeginLoading())
                return;

            try
            {
                await foreach (ResourceGroupResource rg in AzureResourceService.Instance.GetResourceGroupsAsync(SubscriptionId))
                {
                    AddChild(new ResourceGroupNode(rg.Data.Name, SubscriptionId));
                }
            }
            finally
            {
                EndLoading();
            }
        }
    }
}
