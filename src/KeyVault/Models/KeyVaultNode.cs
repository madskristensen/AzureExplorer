using System.Collections.Generic;
using System.Linq;
using System.Threading;

using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.KeyVault.Models
{
    internal enum KeyVaultState
    {
        Unknown,
        Succeeded,
        Failed
    }

    /// <summary>
    /// Represents an Azure Key Vault. Expandable node containing secrets directly.
    /// </summary>
    internal sealed class KeyVaultNode : ExplorerNodeBase, IPortalResource
    {
        private KeyVaultState _state;

        public KeyVaultNode(string name, string subscriptionId, string resourceGroupName, string state, string vaultUri)
            : base(name)
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            // Construct URI from vault name if not provided
            VaultUri = vaultUri ?? $"https://{name}.vault.azure.net/";
            State = ParseState(state);
            Description = State.ToString();

            // Add loading placeholder
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }
        public string VaultUri { get; }

        // IPortalResource
        public string ResourceName => Label;
        public string AzureResourceProvider => "Microsoft.KeyVault/vaults";

        public KeyVaultState State
        {
            get => _state;
            set
            {
                if (SetProperty(ref _state, value))
                {
                    Description = value.ToString();
                    OnPropertyChanged(nameof(IconMoniker));
                }
            }
        }

        public override ImageMoniker IconMoniker => State switch
        {
            KeyVaultState.Succeeded => KnownMonikers.AzureKeyVault,
            KeyVaultState.Failed => KnownMonikers.ApplicationWarning,
            _ => KnownMonikers.AzureKeyVault
        };

        public override int ContextMenuId => PackageIds.KeyVaultContextMenu;
        public override bool SupportsChildren => true;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            try
            {
                var secrets = new List<SecretNode>();

                await foreach (SecretNode secret in AzureResourceService.Instance.GetSecretsAsync(
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
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                Children.Clear();
                Children.Add(new LoadingNode { Label = $"Error: {ex.Message}" });
            }
            finally
            {
                EndLoading();
            }
        }

        internal static KeyVaultState ParseState(string state)
        {
            if (string.IsNullOrEmpty(state))
                return KeyVaultState.Unknown;

            if (state.Equals("Succeeded", System.StringComparison.OrdinalIgnoreCase))
                return KeyVaultState.Succeeded;

            if (state.Equals("Failed", System.StringComparison.OrdinalIgnoreCase))
                return KeyVaultState.Failed;

            return KeyVaultState.Unknown;
        }
    }
}
