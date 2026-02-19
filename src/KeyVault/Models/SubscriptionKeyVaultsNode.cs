using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;

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
