using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.KeyVault.Models
{
    /// <summary>
    /// Represents an Azure Key Vault. Expandable node containing Secrets, Keys, and Certificates folders.
    /// </summary>
    internal sealed class KeyVaultNode : ExplorerNodeBase, IPortalResource, ITaggableResource
    {
        private ProvisioningState _state;

        public KeyVaultNode(string name, string subscriptionId, string resourceGroupName, string state, string vaultUri, IDictionary<string, string> tags = null)
            : base(name)
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            // Construct URI from vault name if not provided
            VaultUri = vaultUri ?? $"https://{name}.vault.azure.net/";
            _state = ProvisioningStateParser.Parse(state);
            Description = _state == ProvisioningState.Failed ? _state.ToString() : null;

            // Store tags, filtering out Azure system/internal tags
            IDictionary<string, string> filteredTags = tags?.FilterUserTags();
            Tags = filteredTags != null && filteredTags.Count > 0
                ? new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(filteredTags))
                : new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

            // Register tags with TagService for filtering
            if (Tags.Count > 0)
            {
                TagService.Instance.RegisterTags(Tags);
            }

            // Add loading placeholder
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }
        public string VaultUri { get; }

        // IPortalResource
        public string ResourceName => Label;
        public string AzureResourceProvider => "Microsoft.KeyVault/vaults";

        // ITaggableResource
        public IReadOnlyDictionary<string, string> Tags { get; }
        public string TagsTooltip => Tags.FormatTagsTooltip();
        public bool HasTag(string key, string value = null) => Tags.ContainsTag(key, value);

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

            await LoadChildrenWithErrorHandlingAsync(_ =>
            {
                // Add Tags node if resource has tags
                if (Tags.Count > 0)
                {
                    AddChild(new TagsNode(Tags));
                }

                // Add folder nodes for Secrets, Keys, and Certificates
                AddChild(new SecretsNode(SubscriptionId, VaultUri));
                AddChild(new KeysNode(SubscriptionId, VaultUri));
                AddChild(new CertificatesNode(SubscriptionId, VaultUri));

                return Task.CompletedTask;
            }, cancellationToken);
        }
    }
}
