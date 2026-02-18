using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using AzureExplorer.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Models
{
    /// <summary>
    /// Category node that groups secrets under a Key Vault.
    /// </summary>
    internal sealed class SecretsNode : ExplorerNodeBase
    {
        public SecretsNode(string subscriptionId, string vaultName, string vaultUri)
            : base("Secrets")
        {
            SubscriptionId = subscriptionId;
            VaultName = vaultName;
            VaultUri = vaultUri;
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }
        public string VaultName { get; }
        public string VaultUri { get; }

        public override ImageMoniker IconMoniker => KnownMonikers.FolderClosed;
        public override int ContextMenuId => 0; // No context menu for now
        public override bool SupportsChildren => true;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            try
            {
                var secrets = new List<SecretNode>();

                await foreach (var secret in AzureResourceService.Instance.GetSecretsAsync(
                    SubscriptionId, VaultUri, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    secrets.Add(secret);
                }

                // Sort alphabetically by name
                foreach (SecretNode node in secrets.OrderBy(s => s.Label, StringComparer.OrdinalIgnoreCase))
                {
                    AddChild(node);
                }

                // Update parent description with count
                Description = $"({secrets.Count})";
            }
            catch (Exception ex)
            {
                if (Children.Count <= 1)
                {
                    Children.Clear();
                    Children.Add(new LoadingNode { Label = $"Error: {ex.Message}" });
                    IsLoading = false;
                    IsLoaded = true;
                    return;
                }
            }

            EndLoading();
        }
    }
}
