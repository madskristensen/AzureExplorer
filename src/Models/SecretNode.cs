using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Models
{
    /// <summary>
    /// Represents an individual secret within an Azure Key Vault.
    /// </summary>
    internal sealed class SecretNode : ExplorerNodeBase
    {
        public SecretNode(string name, string vaultUri, bool enabled)
            : base(name)
        {
            VaultUri = vaultUri;
            Enabled = enabled;
            Description = enabled ? "Enabled" : "Disabled";
        }

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
    }
}
