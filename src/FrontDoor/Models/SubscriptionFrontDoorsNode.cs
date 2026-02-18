using Azure.ResourceManager.Resources;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Models
{
    /// <summary>
    /// Category node that lists all Azure Front Door profiles across the entire subscription.
    /// </summary>
    internal sealed class SubscriptionFrontDoorsNode : SubscriptionResourceNodeBase
    {
        public SubscriptionFrontDoorsNode(string subscriptionId)
            : base("Front Doors", subscriptionId)
        {
        }

        protected override string ResourceType => "Microsoft.Cdn/profiles";

        public override ImageMoniker IconMoniker => KnownMonikers.CloudGroup;
        public override int ContextMenuId => PackageIds.FrontDoorsCategoryContextMenu;

        protected override ExplorerNodeBase CreateNodeFromResource(string name, string resourceGroup, GenericResource resource)
        {
            // GenericResource doesn't have Front Door specific properties,
            // so we create with minimal info - details load when expanded
            return new FrontDoorNode(name, SubscriptionId, resourceGroup, state: null, hostName: null);
        }
    }
}
