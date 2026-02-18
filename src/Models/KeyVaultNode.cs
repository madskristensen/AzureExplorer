using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Models
{
    internal enum KeyVaultState
    {
        Unknown,
        Succeeded,
        Failed
    }

    /// <summary>
    /// Represents an Azure Key Vault. Leaf node with context menu actions.
    /// </summary>
    internal sealed class KeyVaultNode : ExplorerNodeBase
    {
        private KeyVaultState _state;

        public KeyVaultNode(string name, string subscriptionId, string resourceGroupName, string state, string vaultUri)
            : base(name)
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            VaultUri = vaultUri;
            State = ParseState(state);
            Description = State.ToString();
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }
        public string VaultUri { get; }

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
            KeyVaultState.Succeeded => KnownMonikers.Key,
            KeyVaultState.Failed => KnownMonikers.ApplicationWarning,
            _ => KnownMonikers.Key
        };

        public override int ContextMenuId => PackageIds.KeyVaultContextMenu;
        public override bool SupportsChildren => false;

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
