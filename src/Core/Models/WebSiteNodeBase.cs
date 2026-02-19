using System.Threading;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Core.Models
{
    /// <summary>
    /// Represents the state of an Azure Web Site (App Service or Function App).
    /// </summary>
    internal enum WebSiteState
    {
        Unknown,
        Running,
        Stopped
    }

    /// <summary>
    /// Abstract base class for Azure Web Sites (App Services and Function Apps) that share
    /// common properties, state management, and file browsing capabilities.
    /// </summary>
    internal abstract class WebSiteNodeBase : ExplorerNodeBase, IPortalResource, IWebSiteNode
    {
        private WebSiteState _state;

        protected WebSiteNodeBase(string name, string subscriptionId, string resourceGroupName, string state, string defaultHostName)
            : base(name)
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            DefaultHostName = defaultHostName;
            BrowseUrl = string.IsNullOrEmpty(defaultHostName) ? null : $"https://{defaultHostName}";
            _state = ParseState(state);
            Description = _state == WebSiteState.Stopped ? _state.ToString() : null;

            // Add loading placeholder for expandable node
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }
        public string DefaultHostName { get; }
        public string BrowseUrl { get; }

        // IPortalResource
        public string ResourceName => Label;
        public string AzureResourceProvider => "Microsoft.Web/sites";

        public WebSiteState State
        {
            get => _state;
            set
            {
                if (SetProperty(ref _state, value))
                {
                    // Only show description for non-normal states (Stopped)
                    // Don't show "Unknown" or "Running" as they're not useful
                    Description = value == WebSiteState.Stopped ? value.ToString() : null;
                    OnPropertyChanged(nameof(IconMoniker));
                }
            }
        }

        /// <summary>
        /// The icon to display when the site is running.
        /// </summary>
        protected abstract ImageMoniker RunningIconMoniker { get; }

        public override ImageMoniker IconMoniker => State switch
        {
            WebSiteState.Running => RunningIconMoniker,
            WebSiteState.Stopped => KnownMonikers.CloudStopped,
            _ => RunningIconMoniker
        };

        public override bool SupportsChildren => true;

        /// <summary>
        /// Parses the state string from Azure into a <see cref="WebSiteState"/> enum value.
        /// </summary>
        public static WebSiteState ParseState(string state)
        {
            if (string.IsNullOrEmpty(state))
                return WebSiteState.Unknown;

            if (state.Equals("Running", System.StringComparison.OrdinalIgnoreCase))
                return WebSiteState.Running;

            if (state.Equals("Stopped", System.StringComparison.OrdinalIgnoreCase))
                return WebSiteState.Stopped;

            return WebSiteState.Unknown;
        }
    }
}
