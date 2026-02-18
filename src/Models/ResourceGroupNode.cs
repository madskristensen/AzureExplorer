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

        public override Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return Task.CompletedTask;

            // Add category nodes for each resource type
            AddChild(new AppServicesNode(SubscriptionId, ResourceGroupName));
            AddChild(new AppServicePlansNode(SubscriptionId, ResourceGroupName));
            AddChild(new FrontDoorsNode(SubscriptionId, ResourceGroupName));

            EndLoading();
            return Task.CompletedTask;
        }
    }
}
