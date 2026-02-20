using System;

using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.KeyVault.Models
{
    /// <summary>
    /// Category node that lists all Azure Key Vaults across the entire subscription.
    /// </summary>
    internal sealed class SubscriptionKeyVaultsNode : SubscriptionResourceNodeBase
    {
        public SubscriptionKeyVaultsNode(string subscriptionId) : base("Key Vaults", subscriptionId)
        {
            // Subscribe to resource events to sync across views
            ResourceNotificationService.ResourceCreated += OnResourceCreated;
            ResourceNotificationService.ResourceDeleted += OnResourceDeleted;
        }

        protected override string ResourceType => "Microsoft.KeyVault/vaults";

        public override ImageMoniker IconMoniker => KnownMonikers.AzureKeyVault;
        public override int ContextMenuId => PackageIds.SubscriptionKeyVaultsCategoryContextMenu;

        private void OnResourceCreated(object sender, ResourceCreatedEventArgs e)
        {
            if (!ShouldHandleEvent(e.ResourceType, e.SubscriptionId))
                return;

            if (!IsLoaded)
                return;

            // Check if already exists
            foreach (var child in Children)
            {
                if (child is KeyVaultNode existing &&
                    string.Equals(existing.Label, e.ResourceName, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            // Add the new key vault
            var newNode = new KeyVaultNode(
                e.ResourceName,
                SubscriptionId,
                e.ResourceGroupName,
                "Succeeded",
                null); // VaultUri will be constructed from name
            InsertChildSorted(newNode);
        }

        private void OnResourceDeleted(object sender, ResourceDeletedEventArgs e)
        {
            if (!ShouldHandleEvent(e.ResourceType, e.SubscriptionId))
                return;

            // Find and remove the matching child node
            for (int i = Children.Count - 1; i >= 0; i--)
            {
                if (Children[i] is KeyVaultNode node &&
                    string.Equals(node.Label, e.ResourceName, StringComparison.OrdinalIgnoreCase))
                {
                    Children.RemoveAt(i);
                    break;
                }
            }
        }

        private bool ShouldHandleEvent(string resourceType, string subscriptionId)
        {
            return string.Equals(resourceType, ResourceType, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(subscriptionId, SubscriptionId, StringComparison.OrdinalIgnoreCase);
        }

        protected override ExplorerNodeBase CreateNodeFromGraphResult(ResourceGraphResult resource)
        {
            var state = resource.GetProperty("provisioningState");
            var vaultUri = resource.GetProperty("vaultUri");

            return new KeyVaultNode(
                resource.Name,
                SubscriptionId,
                resource.ResourceGroup,
                state,
                vaultUri,
                resource.Tags);
        }
    }
}
