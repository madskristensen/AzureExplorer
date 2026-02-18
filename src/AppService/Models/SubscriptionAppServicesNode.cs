using Azure.ResourceManager.Resources;

using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.AppService.Models
{
    /// <summary>
    /// Category node that lists all App Services across the entire subscription.
    /// </summary>
    internal sealed class SubscriptionAppServicesNode : SubscriptionResourceNodeBase
    {
        public SubscriptionAppServicesNode(string subscriptionId)
            : base("App Services", subscriptionId)
        {
        }

        protected override string ResourceType => "Microsoft.Web/sites";

        public override ImageMoniker IconMoniker => KnownMonikers.Web;
        public override int ContextMenuId => PackageIds.AppServicesCategoryContextMenu;

        protected override ExplorerNodeBase CreateNodeFromResource(string name, string resourceGroup, GenericResource resource)
        {
            // GenericResource doesn't have App Service specific properties,
            // so we create with minimal info - details load when expanded
            return new AppServiceNode(name, SubscriptionId, resourceGroup, state: null, defaultHostName: null);
        }
    }
}
