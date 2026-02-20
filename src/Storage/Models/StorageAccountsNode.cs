using System.Collections.Generic;
using System.Linq;
using System.Threading;

using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Storage.Models
{
    /// <summary>
    /// Category node that groups Azure Storage Accounts under a resource group.
    /// </summary>
    internal sealed class StorageAccountsNode : ExplorerNodeBase
    {
        public StorageAccountsNode(string subscriptionId, string resourceGroupName)
            : base("Storage Accounts")
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            Children.Add(new LoadingNode());

            // Subscribe to resource events to sync with other views
            ResourceNotificationService.ResourceCreated += OnResourceCreated;
            ResourceNotificationService.ResourceDeleted += OnResourceDeleted;
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }

        public override ImageMoniker IconMoniker => KnownMonikers.AzureStorageAccount;
        public override int ContextMenuId => PackageIds.StorageAccountsCategoryContextMenu;
        public override bool SupportsChildren => true;

        private void OnResourceCreated(object sender, ResourceCreatedEventArgs e)
        {
            // Only handle storage account creations in our subscription/resource group
            if (!string.Equals(e.ResourceType, "Microsoft.Storage/storageAccounts", System.StringComparison.OrdinalIgnoreCase))
                return;

            if (!string.Equals(e.SubscriptionId, SubscriptionId, System.StringComparison.OrdinalIgnoreCase))
                return;

            if (!string.Equals(e.ResourceGroupName, ResourceGroupName, System.StringComparison.OrdinalIgnoreCase))
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
                ResourceGroupName,
                "Succeeded",
                "StorageV2",
                skuName);
            InsertChildSorted(newNode);
        }

        private void OnResourceDeleted(object sender, ResourceDeletedEventArgs e)
        {
            // Only handle storage account deletions in our subscription/resource group
            if (!string.Equals(e.ResourceType, "Microsoft.Storage/storageAccounts", System.StringComparison.OrdinalIgnoreCase))
                return;

            if (!string.Equals(e.SubscriptionId, SubscriptionId, System.StringComparison.OrdinalIgnoreCase))
                return;

            if (!string.Equals(e.ResourceGroupName, ResourceGroupName, System.StringComparison.OrdinalIgnoreCase))
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

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            try
            {
                // Use Azure Resource Graph for fast loading
                IReadOnlyList<ResourceGraphResult> resources = await ResourceGraphService.Instance.QueryByTypeAsync(
                    SubscriptionId,
                    "Microsoft.Storage/storageAccounts",
                    ResourceGroupName,
                    cancellationToken);

                foreach (ResourceGraphResult resource in resources.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var state = resource.GetProperty("provisioningState");

                    AddChild(new StorageAccountNode(
                        resource.Name,
                        SubscriptionId,
                        ResourceGroupName,
                        state,
                        resource.Kind,
                        resource.SkuName,
                        resource.Tags));
                }
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                Children.Clear();
                Children.Add(new LoadingNode { Label = $"Error: {ex.Message}" });
            }
            finally
            {
                EndLoading();
            }
        }

        /// <summary>
        /// Adds a new storage account node in sorted order without refreshing existing nodes.
        /// </summary>
        /// <returns>The newly created node.</returns>
        public StorageAccountNode AddStorageAccount(string name, string skuName)
        {
            var newNode = new StorageAccountNode(
                name,
                SubscriptionId,
                ResourceGroupName,
                "Succeeded",
                "StorageV2",
                skuName);
            InsertChildSorted(newNode);
            return newNode;
        }
    }
}
