using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Storage.Models
{
    /// <summary>
    /// Category node that lists all Azure Storage Accounts across the entire subscription.
    /// </summary>
    internal sealed class SubscriptionStorageAccountsNode : SubscriptionResourceNodeBase
    {
        public SubscriptionStorageAccountsNode(string subscriptionId) : base("Storage Accounts", subscriptionId)
        {
            // Subscribe to resource events to sync with other views
            ResourceNotificationService.ResourceCreated += OnResourceCreated;
            ResourceNotificationService.ResourceDeleted += OnResourceDeleted;
        }

        protected override string ResourceType => "Microsoft.Storage/storageAccounts";

        public override ImageMoniker IconMoniker => KnownMonikers.AzureStorageAccount;
        public override int ContextMenuId => PackageIds.SubscriptionStorageAccountsCategoryContextMenu;

        private void OnResourceCreated(object sender, ResourceCreatedEventArgs e)
        {
            // Only handle storage account creations in our subscription
            if (!string.Equals(e.ResourceType, "Microsoft.Storage/storageAccounts", System.StringComparison.OrdinalIgnoreCase))
                return;

            if (!string.Equals(e.SubscriptionId, SubscriptionId, System.StringComparison.OrdinalIgnoreCase))
                return;

            // Don't add if not yet loaded or if already exists
            if (!IsLoaded)
                return;

            foreach (var child in Children)
            {
                if (child is StorageAccountNode existing &&
                    string.Equals(existing.Label, e.ResourceName, System.StringComparison.OrdinalIgnoreCase))
                    return; // Already exists
            }

            // Add the new storage account
            var skuName = e.AdditionalData as string ?? "Standard_LRS";
            var newNode = new StorageAccountNode(
                e.ResourceName,
                SubscriptionId,
                e.ResourceGroupName,
                "Succeeded",
                "StorageV2",
                skuName);
            InsertChildSorted(newNode);
        }

        private void OnResourceDeleted(object sender, ResourceDeletedEventArgs e)
        {
            // Only handle storage account deletions in our subscription
            if (!string.Equals(e.ResourceType, "Microsoft.Storage/storageAccounts", System.StringComparison.OrdinalIgnoreCase))
                return;

            if (!string.Equals(e.SubscriptionId, SubscriptionId, System.StringComparison.OrdinalIgnoreCase))
                return;

            // Find and remove the matching child node
            for (int i = Children.Count - 1; i >= 0; i--)
            {
                if (Children[i] is StorageAccountNode node &&
                    string.Equals(node.Label, e.ResourceName, System.StringComparison.OrdinalIgnoreCase))
                {
                    Children.RemoveAt(i);
                    break;
                }
            }
        }

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
