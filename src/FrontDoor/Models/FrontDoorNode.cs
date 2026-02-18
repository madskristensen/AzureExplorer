using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.FrontDoor.Models
{
    internal enum FrontDoorState
    {
        Unknown,
        Active,
        Disabled
    }

    /// <summary>
    /// Represents an Azure Front Door profile. Leaf node with context menu actions.
    /// </summary>
    internal sealed class FrontDoorNode : ExplorerNodeBase, IPortalResource
    {
        private FrontDoorState _state;

        public FrontDoorNode(string name, string subscriptionId, string resourceGroupName, string state, string hostName)
            : base(name)
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            HostName = hostName;
            BrowseUrl = string.IsNullOrEmpty(hostName) ? null : $"https://{hostName}";
            _state = ParseState(state);
            Description = _state == FrontDoorState.Disabled ? _state.ToString() : null;
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }
        public string HostName { get; }
        public string BrowseUrl { get; }

        // IPortalResource
        public string ResourceName => Label;
        public string AzureResourceProvider => "Microsoft.Cdn/profiles";

        public FrontDoorState State
        {
            get => _state;
            set
            {
                if (SetProperty(ref _state, value))
                {
                    // Only show description for non-normal states (Disabled)
                    // Don't show "Unknown" or "Active" as they're not useful
                    Description = value == FrontDoorState.Disabled ? value.ToString() : null;
                    OnPropertyChanged(nameof(IconMoniker));
                }
            }
        }

        public override ImageMoniker IconMoniker => State switch
        {
            FrontDoorState.Active => KnownMonikers.CloudGroup,
            FrontDoorState.Disabled => KnownMonikers.ApplicationWarning,
            _ => KnownMonikers.CloudGroup
        };

        public override int ContextMenuId => PackageIds.FrontDoorContextMenu;
        public override bool SupportsChildren => false;

        internal static FrontDoorState ParseState(string state)
        {
            if (string.IsNullOrEmpty(state))
                return FrontDoorState.Unknown;

            if (state.Equals("Active", System.StringComparison.OrdinalIgnoreCase) ||
                state.Equals("Enabled", System.StringComparison.OrdinalIgnoreCase))
                return FrontDoorState.Active;

            if (state.Equals("Disabled", System.StringComparison.OrdinalIgnoreCase))
                return FrontDoorState.Disabled;

            return FrontDoorState.Unknown;
        }
    }
}
