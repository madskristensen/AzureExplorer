using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.KeyVault.Models
{
    /// <summary>
    /// Container node representing the "Certificates" section under a Key Vault.
    /// Shows certificates when expanded.
    /// </summary>
    internal sealed class CertificatesNode : ExplorerNodeBase
    {
        public CertificatesNode(string subscriptionId, string vaultUri)
            : base("Certificates")
        {
            SubscriptionId = subscriptionId;
            VaultUri = vaultUri;

            // Add loading placeholder
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }
        public string VaultUri { get; }

        public override ImageMoniker IconMoniker => KnownMonikers.Certificate;
        public override int ContextMenuId => 0; // No context menu for certificates list
        public override bool SupportsChildren => true;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            await LoadChildrenWithErrorHandlingAsync(async ct =>
            {
                var certificates = new List<CertificateNode>();

                await foreach (CertificateNode cert in AzureResourceService.Instance.GetCertificatesAsync(
                    SubscriptionId, VaultUri, ct))
                {
                    ct.ThrowIfCancellationRequested();
                    certificates.Add(cert);
                }

                // Sort alphabetically by name
                foreach (CertificateNode node in certificates.OrderBy(c => c.Label, StringComparer.OrdinalIgnoreCase))
                {
                    AddChild(node);
                }
            }, cancellationToken);
        }
    }
}
