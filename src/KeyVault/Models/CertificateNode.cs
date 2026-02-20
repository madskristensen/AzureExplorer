using System;

using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.KeyVault.Models
{
    /// <summary>
    /// Represents an individual certificate within an Azure Key Vault.
    /// </summary>
    internal sealed class CertificateNode : ExplorerNodeBase
    {
        public CertificateNode(string name, string subscriptionId, string vaultUri, bool enabled, DateTimeOffset? expiresOn)
            : base(name)
        {
            SubscriptionId = subscriptionId;
            VaultUri = vaultUri;
            Enabled = enabled;
            ExpiresOn = expiresOn;
            Description = FormatDescription(enabled, expiresOn);
        }

        public string SubscriptionId { get; }
        public string VaultUri { get; }
        public bool Enabled { get; }
        public DateTimeOffset? ExpiresOn { get; }

        /// <summary>
        /// The full certificate identifier URL.
        /// </summary>
        public string CertificateId => $"{VaultUri.TrimEnd('/')}/certificates/{Label}";

        /// <summary>
        /// Returns true if the certificate has expired.
        /// </summary>
        public bool IsExpired => ExpiresOn.HasValue && ExpiresOn.Value < DateTimeOffset.UtcNow;

        public override ImageMoniker IconMoniker
        {
            get
            {
                if (!Enabled || IsExpired)
                    return KnownMonikers.StatusWarning;
                return KnownMonikers.Certificate;
            }
        }

        public override int ContextMenuId => PackageIds.CertificateContextMenu;
        public override bool SupportsChildren => false;

        private static string FormatDescription(bool enabled, DateTimeOffset? expiresOn)
        {
            var status = enabled ? "Enabled" : "Disabled";

            if (expiresOn.HasValue)
            {
                if (expiresOn.Value < DateTimeOffset.UtcNow)
                {
                    return $"Expired ({status})";
                }
                return $"Expires {expiresOn.Value:yyyy-MM-dd} ({status})";
            }

            return status;
        }
    }
}
