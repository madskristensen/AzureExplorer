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
    /// Container node representing the "Secrets" section under a Key Vault.
    /// Shows secrets when expanded.
    /// </summary>
    internal sealed class SecretsNode : ExplorerNodeBase
    {
        public SecretsNode(string subscriptionId, string vaultUri)
            : base("Secrets")
        {
            SubscriptionId = subscriptionId;
            VaultUri = vaultUri;

            // Add loading placeholder
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }
        public string VaultUri { get; }

        public override ImageMoniker IconMoniker => KnownMonikers.HiddenField;
        public override int ContextMenuId => PackageIds.SecretsNodeContextMenu;
        public override bool SupportsChildren => true;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            await LoadChildrenWithErrorHandlingAsync(async ct =>
            {
                var secrets = new List<SecretNode>();

                await foreach (SecretNode secret in AzureResourceService.Instance.GetSecretsAsync(
                    SubscriptionId, VaultUri, ct))
                {
                    ct.ThrowIfCancellationRequested();
                    secrets.Add(secret);
                }

                // Sort alphabetically by name
                foreach (SecretNode node in secrets.OrderBy(s => s.Label, StringComparer.OrdinalIgnoreCase))
                {
                    AddChild(node);
                }
            }, cancellationToken);
        }
    }
}
