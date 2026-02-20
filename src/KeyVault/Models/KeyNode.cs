using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.KeyVault.Models
{
    /// <summary>
    /// Represents an individual cryptographic key within an Azure Key Vault.
    /// </summary>
    internal sealed class KeyNode : ExplorerNodeBase
    {
        public KeyNode(string name, string subscriptionId, string vaultUri, bool enabled, string keyType)
            : base(name)
        {
            SubscriptionId = subscriptionId;
            VaultUri = vaultUri;
            Enabled = enabled;
            KeyType = keyType;
            Description = FormatDescription(enabled, keyType);
        }

        public string SubscriptionId { get; }
        public string VaultUri { get; }
        public bool Enabled { get; }
        public string KeyType { get; }

        /// <summary>
        /// The full key identifier URL.
        /// </summary>
        public string KeyId => $"{VaultUri.TrimEnd('/')}/keys/{Label}";

        public override ImageMoniker IconMoniker => Enabled
            ? KnownMonikers.Key
            : KnownMonikers.StatusWarning;

        public override int ContextMenuId => PackageIds.KeyContextMenu;
        public override bool SupportsChildren => false;

        private static string FormatDescription(bool enabled, string keyType)
        {
            var status = enabled ? "Enabled" : "Disabled";
            return string.IsNullOrEmpty(keyType) ? status : $"{keyType} ({status})";
        }
    }
}
