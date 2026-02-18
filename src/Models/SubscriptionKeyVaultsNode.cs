using Azure.ResourceManager.Resources;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Models
{
    /// <summary>
    /// Category node that lists all Azure Key Vaults across the entire subscription.
    /// </summary>
    internal sealed class SubscriptionKeyVaultsNode : SubscriptionResourceNodeBase
    {
        public SubscriptionKeyVaultsNode(string subscriptionId)
            : base("Key Vaults", subscriptionId)
        {
        }

        protected override string ResourceType => "Microsoft.KeyVault/vaults";

        public override ImageMoniker IconMoniker => KnownMonikers.Key;
        public override int ContextMenuId => PackageIds.KeyVaultsCategoryContextMenu;

        protected override ExplorerNodeBase CreateNodeFromResource(string name, string resourceGroup, GenericResource resource)
        {
            // GenericResource doesn't have Key Vault specific properties,
            // so we create with minimal info - details load when expanded
            return new KeyVaultNode(name, SubscriptionId, resourceGroup, state: null, vaultUri: null);
        }
    }
}
