using System.Threading.Tasks;

using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.KeyVault.Models
{
    /// <summary>
    /// Represents an individual secret within an Azure Key Vault.
    /// </summary>
    internal sealed class SecretNode : ExplorerNodeBase, IDeletableResource
    {
        public SecretNode(string name, string subscriptionId, string vaultUri, bool enabled)
            : base(name)
        {
            SubscriptionId = subscriptionId;
            VaultUri = vaultUri;
            Enabled = enabled;
            Description = enabled ? "Enabled" : "Disabled";
        }

        public string SubscriptionId { get; }
        public string VaultUri { get; }
        public bool Enabled { get; }

        /// <summary>
        /// The full secret identifier URL.
        /// </summary>
        public string SecretId => $"{VaultUri.TrimEnd('/')}/secrets/{Label}";

        public override ImageMoniker IconMoniker => Enabled
            ? KnownMonikers.Key
            : KnownMonikers.StatusWarning;

        public override int ContextMenuId => PackageIds.SecretContextMenu;
        public override bool SupportsChildren => false;

        // IDeletableResource implementation
        string IDeletableResource.DeleteResourceType => "Secret";
        string IDeletableResource.DeleteResourceName => Label;
        string IDeletableResource.DeleteResourceProvider => null; // Secrets don't appear in multiple views
        string IDeletableResource.DeleteSubscriptionId => null;
        string IDeletableResource.DeleteResourceGroupName => null;

        async Task IDeletableResource.DeleteAsync()
        {
            await AzureResourceService.Instance.DeleteSecretAsync(SubscriptionId, VaultUri, Label);
        }
    }
}
