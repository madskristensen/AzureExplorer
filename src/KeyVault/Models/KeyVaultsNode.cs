using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.KeyVault.Models
{
    /// <summary>
    /// Category node that groups Azure Key Vaults under a resource group.
    /// </summary>
    internal sealed class KeyVaultsNode : ExplorerNodeBase
    {
        private const string ResourceProvider = "Microsoft.KeyVault/vaults";

        public KeyVaultsNode(string subscriptionId, string resourceGroupName)
            : base("Key Vaults")
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            Children.Add(new LoadingNode());

            // Subscribe to resource events to sync across views
            ResourceNotificationService.ResourceCreated += OnResourceCreated;
            ResourceNotificationService.ResourceDeleted += OnResourceDeleted;
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }

        public override ImageMoniker IconMoniker => KnownMonikers.AzureKeyVault;
        public override int ContextMenuId => PackageIds.KeyVaultsCategoryContextMenu;
        public override bool SupportsChildren => true;

        private void OnResourceCreated(object sender, ResourceCreatedEventArgs e)
        {
            if (!ShouldHandleEvent(e.ResourceType, e.SubscriptionId, e.ResourceGroupName))
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
                ResourceGroupName,
                "Succeeded",
                null); // VaultUri will be constructed from name
            InsertChildSorted(newNode);
        }

        private void OnResourceDeleted(object sender, ResourceDeletedEventArgs e)
        {
            if (!ShouldHandleEvent(e.ResourceType, e.SubscriptionId, e.ResourceGroupName))
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

        private bool ShouldHandleEvent(string resourceType, string subscriptionId, string resourceGroupName)
        {
            return string.Equals(resourceType, ResourceProvider, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(subscriptionId, SubscriptionId, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(resourceGroupName, ResourceGroupName, StringComparison.OrdinalIgnoreCase);
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
                    ResourceProvider,
                    ResourceGroupName,
                    cancellationToken);

                foreach (ResourceGraphResult resource in resources.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var state = resource.GetProperty("provisioningState");
                    var vaultUri = resource.GetProperty("vaultUri");

                    AddChild(new KeyVaultNode(
                        resource.Name,
                        SubscriptionId,
                        ResourceGroupName,
                        state,
                        vaultUri,
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
        /// Adds a new key vault node in sorted order without refreshing existing nodes.
        /// </summary>
        /// <returns>The newly created node.</returns>
        public KeyVaultNode AddKeyVault(string name)
        {
            var newNode = new KeyVaultNode(name, SubscriptionId, ResourceGroupName, "Succeeded", null);
            InsertChildSorted(newNode);
            return newNode;
        }
    }
}
