using System.Text.Json;

using Azure.ResourceManager.Resources;

using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.AppService.Models
{
    /// <summary>
    /// Category node that lists all App Services across the entire subscription.
    /// </summary>
    internal sealed class SubscriptionAppServicesNode(string subscriptionId) : SubscriptionResourceNodeBase("App Services", subscriptionId)
    {
        protected override string ResourceType => "Microsoft.Web/sites";

        public override ImageMoniker IconMoniker => KnownMonikers.AzureWebSites;
        public override int ContextMenuId => PackageIds.AppServicesCategoryContextMenu;

        protected override bool ShouldIncludeResource(ResourceGraphResult resource)
        {
            // Exclude Function Apps (they have their own category)
            var kind = resource.Kind;
            if (!string.IsNullOrEmpty(kind) && kind.IndexOf("functionapp", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            return true;
        }

        protected override bool ShouldIncludeResourceArm(GenericResource resource)
        {
            // Exclude Function Apps (they have their own category)
            var kind = resource.Data.Kind;
            if (!string.IsNullOrEmpty(kind) && kind.IndexOf("functionapp", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            return true;
        }

        protected override ExplorerNodeBase CreateNodeFromGraphResult(ResourceGraphResult resource)
        {
            var state = resource.GetProperty("state");
            var defaultHostName = resource.GetProperty("defaultHostName");

            return new AppServiceNode(
                resource.Name,
                SubscriptionId,
                resource.ResourceGroup,
                state,
                defaultHostName,
                resource.Tags);
        }

        protected override ExplorerNodeBase CreateNodeFromArmResource(GenericResource resource)
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
                        state = stateElement.GetString();

                    if (root.TryGetProperty("defaultHostName", out JsonElement hostElement))
                        defaultHostName = hostElement.GetString();
                }
                catch { }
            }

            return new AppServiceNode(
                resource.Data.Name,
                SubscriptionId,
                resource.Id.ResourceGroupName,
                state,
                defaultHostName,
                resource.Data.Tags);
        }
    }
}
