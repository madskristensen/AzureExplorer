using System.Text.Json;

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
            string state = null;
            string defaultHostName = null;

            if (resource.Data.Properties != null)
            {
                try
                {
                    using var doc = JsonDocument.Parse(resource.Data.Properties);
                    JsonElement root = doc.RootElement;

                    if (root.TryGetProperty("state", out JsonElement stateElement))
                    {
                        state = stateElement.GetString();
                    }

                    if (root.TryGetProperty("defaultHostName", out JsonElement hostElement))
                    {
                        defaultHostName = hostElement.GetString();
                    }
                }
                catch
                {
                    // Properties parsing failed; continue with null values
                }
            }

            return new AppServiceNode(name, SubscriptionId, resourceGroup, state, defaultHostName);
        }
    }
}
