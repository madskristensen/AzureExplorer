using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;

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
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }

        public override ImageMoniker IconMoniker => KnownMonikers.AzureStorageAccount;
        public override int ContextMenuId => PackageIds.StorageAccountsCategoryContextMenu;
        public override bool SupportsChildren => true;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            try
            {
                ArmClient client = AzureResourceService.Instance.GetClient(SubscriptionId);
                SubscriptionResource sub = client.GetSubscriptionResource(
                    SubscriptionResource.CreateResourceIdentifier(SubscriptionId));
                ResourceGroupResource rg = (await sub.GetResourceGroupAsync(ResourceGroupName, cancellationToken)).Value;

                var storageAccounts = new List<StorageAccountNode>();

                await foreach (StorageAccountResource account in rg.GetStorageAccounts().GetAllAsync(cancellationToken: cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    storageAccounts.Add(new StorageAccountNode(
                        account.Data.Name,
                        SubscriptionId,
                        ResourceGroupName,
                        account.Data.ProvisioningState?.ToString(),
                        account.Data.Kind?.ToString(),
                        account.Data.Sku?.Name.ToString()));
                }

                // Sort alphabetically by name
                foreach (StorageAccountNode node in storageAccounts.OrderBy(s => s.Label, StringComparer.OrdinalIgnoreCase))
                {
                    AddChild(node);
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
    }
}
