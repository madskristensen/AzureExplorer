using System.Text.Json;

using Azure.ResourceManager.Resources;

using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.FrontDoor.Models
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
            string state = null;

            if (resource.Data.Properties != null)
            {
                try
                {
                    using var doc = JsonDocument.Parse(resource.Data.Properties);
                    JsonElement root = doc.RootElement;

                    if (root.TryGetProperty("resourceState", out JsonElement stateElement))
                    {
                        state = stateElement.GetString();
                    }
                }
                catch
                {
                    // Properties parsing failed; continue with null values
                }
            }

            // Note: hostName requires querying endpoints which is not available in generic resource data
            return new FrontDoorNode(name, SubscriptionId, resourceGroup, state, hostName: null);
        }
    }
}
