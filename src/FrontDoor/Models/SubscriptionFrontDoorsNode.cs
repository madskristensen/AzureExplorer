using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.FrontDoor.Models
{
    /// <summary>
    /// Category node that lists all Azure Front Door profiles across the entire subscription.
    /// </summary>
    internal sealed class SubscriptionFrontDoorsNode(string subscriptionId) : SubscriptionResourceNodeBase("Front Doors", subscriptionId)
    {
        protected override string ResourceType => "Microsoft.Cdn/profiles";

        public override ImageMoniker IconMoniker => KnownMonikers.CloudGroup;
        public override int ContextMenuId => PackageIds.FrontDoorsCategoryContextMenu;

        protected override ExplorerNodeBase CreateNodeFromGraphResult(ResourceGraphResult resource)
        {
            var state = resource.GetProperty("resourceState");

            // Note: hostName requires querying endpoints which is not available in Resource Graph
            return new FrontDoorNode(
                resource.Name,
                SubscriptionId,
                resource.ResourceGroup,
                state,
                hostName: null);
        }
    }
}
