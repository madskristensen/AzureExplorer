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
    /// Container node representing the "Keys" section under a Key Vault.
    /// Shows cryptographic keys when expanded.
    /// </summary>
    internal sealed class KeysNode : ExplorerNodeBase
    {
        public KeysNode(string subscriptionId, string vaultUri)
            : base("Keys")
        {
            SubscriptionId = subscriptionId;
            VaultUri = vaultUri;

            // Add loading placeholder
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }
        public string VaultUri { get; }

        public override ImageMoniker IconMoniker => KnownMonikers.Key;
        public override int ContextMenuId => 0; // No context menu for keys list
        public override bool SupportsChildren => true;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            await LoadChildrenWithErrorHandlingAsync(async ct =>
            {
                var keys = new List<KeyNode>();

                await foreach (KeyNode key in AzureResourceService.Instance.GetKeysAsync(
                    SubscriptionId, VaultUri, ct))
                {
                    ct.ThrowIfCancellationRequested();
                    keys.Add(key);
                }

                // Sort alphabetically by name
                foreach (KeyNode node in keys.OrderBy(k => k.Label, StringComparer.OrdinalIgnoreCase))
                {
                    AddChild(node);
                }
            }, cancellationToken);
        }
    }
}
