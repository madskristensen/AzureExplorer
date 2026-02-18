using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.AppService.Models
{
    internal enum AppServiceState
    {
        Unknown,
        Running,
        Stopped
    }

    /// <summary>
    /// Represents an Azure App Service (Web App). Leaf node with context menu actions.
    /// </summary>
    internal sealed class AppServiceNode : ExplorerNodeBase, IPortalResource
    {
        private AppServiceState _state;

        public AppServiceNode(string name, string subscriptionId, string resourceGroupName, string state, string defaultHostName)
            : base(name)
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            DefaultHostName = defaultHostName;
            BrowseUrl = string.IsNullOrEmpty(defaultHostName) ? null : $"https://{defaultHostName}";
            State = ParseState(state);
            Description = State.ToString();
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }
        public string DefaultHostName { get; }
        public string BrowseUrl { get; }

        // IPortalResource
        public string ResourceName => Label;
        public string AzureResourceProvider => "Microsoft.Web/sites";

        public AppServiceState State
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
            AppServiceState.Running => KnownMonikers.AzureWebSites,
            AppServiceState.Stopped => KnownMonikers.CloudStopped,
            _ => KnownMonikers.AzureWebSites
        };

        public override int ContextMenuId => PackageIds.AppServiceContextMenu;
        public override bool SupportsChildren => false;

        internal static AppServiceState ParseState(string state)
        {
            if (string.IsNullOrEmpty(state))
                return AppServiceState.Unknown;

            if (state.Equals("Running", System.StringComparison.OrdinalIgnoreCase))
                return AppServiceState.Running;

            if (state.Equals("Stopped", System.StringComparison.OrdinalIgnoreCase))
                return AppServiceState.Stopped;

            return AppServiceState.Unknown;
        }
    }
}
