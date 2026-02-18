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
    internal sealed class FrontDoorNode : ExplorerNodeBase
    {
        private FrontDoorState _state;

        public FrontDoorNode(string name, string subscriptionId, string resourceGroupName, string state, string hostName)
            : base(name)
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            HostName = hostName;
            BrowseUrl = string.IsNullOrEmpty(hostName) ? null : $"https://{hostName}";
            State = ParseState(state);
            Description = State.ToString();
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }
        public string HostName { get; }
        public string BrowseUrl { get; }

        public FrontDoorState State
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
