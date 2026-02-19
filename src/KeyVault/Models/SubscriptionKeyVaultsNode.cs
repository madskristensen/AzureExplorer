using System.Collections.Generic;
using System.Text.Json;

using Azure.ResourceManager.Resources;

using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.KeyVault.Models
{
    /// <summary>
    /// Category node that lists all Azure Key Vaults across the entire subscription.
    /// </summary>
    internal sealed class SubscriptionKeyVaultsNode(string subscriptionId) : SubscriptionResourceNodeBase("Key Vaults", subscriptionId)
    {
        protected override string ResourceType => "Microsoft.KeyVault/vaults";

        public override ImageMoniker IconMoniker => KnownMonikers.AzureKeyVault;
        public override int ContextMenuId => PackageIds.KeyVaultsCategoryContextMenu;

        protected override ExplorerNodeBase CreateNodeFromResource(string name, string resourceGroup, GenericResource resource)
        {
            string state = null;
            string vaultUri = null;

            if (resource.Data.Properties != null)
            {
                try
                {
                    using var doc = JsonDocument.Parse(resource.Data.Properties);
                    JsonElement root = doc.RootElement;

                    if (root.TryGetProperty("provisioningState", out JsonElement stateElement))
                    {
                        state = stateElement.GetString();
                    }

                    if (root.TryGetProperty("vaultUri", out JsonElement uriElement))
                    {
                        vaultUri = uriElement.GetString();
                    }
                }
                catch
                {
                    // Properties parsing failed; continue with null values
                }
            }

            // Extract tags from resource
            IDictionary<string, string> tags = resource.Data.Tags;

            return new KeyVaultNode(name, SubscriptionId, resourceGroup, state, vaultUri, tags);
        }
    }
}
