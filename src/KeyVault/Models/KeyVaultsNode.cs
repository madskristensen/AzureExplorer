using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.Resources;

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
        public KeyVaultsNode(string subscriptionId, string resourceGroupName)
            : base("Key Vaults")
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }

        public override ImageMoniker IconMoniker => KnownMonikers.AzureKeyVault;
        public override int ContextMenuId => PackageIds.KeyVaultsCategoryContextMenu;
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

                var keyVaults = new List<KeyVaultNode>();

                await foreach (KeyVaultResource vault in rg.GetKeyVaults().GetAllAsync(cancellationToken: cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    keyVaults.Add(new KeyVaultNode(
                        vault.Data.Name,
                        SubscriptionId,
                        ResourceGroupName,
                        vault.Data.Properties.ProvisioningState?.ToString(),
                        vault.Data.Properties.VaultUri?.ToString()));
                }

                // Sort alphabetically by name
                foreach (var node in keyVaults.OrderBy(k => k.Label, StringComparer.OrdinalIgnoreCase))
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
