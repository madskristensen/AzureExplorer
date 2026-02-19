using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Storage.Models
{
    /// <summary>
    /// Category node that lists all Azure Storage Accounts across the entire subscription.
    /// </summary>
    internal sealed class SubscriptionStorageAccountsNode(string subscriptionId) : SubscriptionResourceNodeBase("Storage Accounts", subscriptionId)
    {
        protected override string ResourceType => "Microsoft.Storage/storageAccounts";

        public override ImageMoniker IconMoniker => KnownMonikers.AzureStorageAccount;
        public override int ContextMenuId => PackageIds.StorageAccountsCategoryContextMenu;

        protected override ExplorerNodeBase CreateNodeFromGraphResult(ResourceGraphResult resource)
        {
            var state = resource.GetProperty("provisioningState");

            return new StorageAccountNode(
                resource.Name,
                SubscriptionId,
                resource.ResourceGroup,
                state,
                resource.Kind,
                resource.SkuName,
                resource.Tags);
        }
    }
}
