using System.Collections.Generic;
using System.Linq;
using System.Threading;

using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.KeyVault.Models
{
    /// <summary>
    /// Represents an Azure Key Vault. Expandable node containing secrets directly.
    /// </summary>
    internal sealed class KeyVaultNode : ExplorerNodeBase, IPortalResource
    {
        private ProvisioningState _state;

        public KeyVaultNode(string name, string subscriptionId, string resourceGroupName, string state, string vaultUri)
            : base(name)
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            // Construct URI from vault name if not provided
            VaultUri = vaultUri ?? $"https://{name}.vault.azure.net/";
            _state = ProvisioningStateParser.Parse(state);
            Description = _state == ProvisioningState.Failed ? _state.ToString() : null;

            // Add loading placeholder
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }
        public string VaultUri { get; }

        // IPortalResource
        public string ResourceName => Label;
        public string AzureResourceProvider => "Microsoft.KeyVault/vaults";

        public ProvisioningState State
        {
            get => _state;
            set
            {
                if (SetProperty(ref _state, value))
                {
                    // Only show description for non-normal states (Failed)
                    // Don't show "Unknown" or "Succeeded" as they're not useful
                    Description = value == ProvisioningState.Failed ? value.ToString() : null;
                    OnPropertyChanged(nameof(IconMoniker));
                }
            }
        }

        public override ImageMoniker IconMoniker => State switch
        {
            ProvisioningState.Succeeded => KnownMonikers.AzureKeyVault,
            ProvisioningState.Failed => KnownMonikers.ApplicationWarning,
            _ => KnownMonikers.AzureKeyVault
        };

        public override int ContextMenuId => PackageIds.KeyVaultContextMenu;
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
