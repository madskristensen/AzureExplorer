using System.Text.Json;

using Azure.ResourceManager.Resources;

using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Storage.Models
{
    /// <summary>
    /// Category node that lists all Azure Storage Accounts across the entire subscription.
    /// </summary>
    internal sealed class SubscriptionStorageAccountsNode : SubscriptionResourceNodeBase
    {
        public SubscriptionStorageAccountsNode(string subscriptionId)
            : base("Storage Accounts", subscriptionId)
        {
        }

        protected override string ResourceType => "Microsoft.Storage/storageAccounts";

        public override ImageMoniker IconMoniker => KnownMonikers.AzureStorageAccount;
        public override int ContextMenuId => PackageIds.StorageAccountsCategoryContextMenu;

        protected override ExplorerNodeBase CreateNodeFromResource(string name, string resourceGroup, GenericResource resource)
        {
            string state = null;
            string kind = null;
            string skuName = null;

            if (resource.Data.Properties != null)
            {
                try
                {
                    using JsonDocument doc = JsonDocument.Parse(resource.Data.Properties);
                    JsonElement root = doc.RootElement;

                    if (root.TryGetProperty("provisioningState", out JsonElement stateElement))
                    {
                        state = stateElement.GetString();
                    }
                }
                catch
                {
                    // Properties parsing failed; continue with null values
                }
            }

            // Kind and SKU are available from the resource data directly
            kind = resource.Data.Kind;
            if (resource.Data.Sku != null)
            {
                skuName = resource.Data.Sku.Name;
            }

            return new StorageAccountNode(name, SubscriptionId, resourceGroup, state, kind, skuName);
        }
    }
}
