using System.Threading;

using AzureExplorer.AppService.Models;
using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.FunctionApp.Models
{
    internal enum FunctionAppState
    {
        Unknown,
        Running,
        Stopped
    }

    /// <summary>
    /// Represents an Azure Function App. Similar to App Service but with Function-specific icon and context.
    /// </summary>
    internal sealed class FunctionAppNode : ExplorerNodeBase, IPortalResource, IWebSiteNode
    {
        private FunctionAppState _state;

        public FunctionAppNode(string name, string subscriptionId, string resourceGroupName, string state, string defaultHostName)
            : base(name)
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            DefaultHostName = defaultHostName;
            BrowseUrl = string.IsNullOrEmpty(defaultHostName) ? null : $"https://{defaultHostName}";
            _state = ParseState(state);
            Description = _state == FunctionAppState.Stopped ? _state.ToString() : null;

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

        public FunctionAppState State
        {
            get => _state;
            set
            {
                if (SetProperty(ref _state, value))
                {
                    Description = value == FunctionAppState.Stopped ? value.ToString() : null;
                    OnPropertyChanged(nameof(IconMoniker));
                }
            }
        }

        public override ImageMoniker IconMoniker => State switch
        {
            FunctionAppState.Running => KnownMonikers.AzureFunctionsApp,
            FunctionAppState.Stopped => KnownMonikers.CloudStopped,
            _ => KnownMonikers.AzureFunctionsApp
        };

        public override int ContextMenuId => PackageIds.FunctionAppContextMenu;
        public override bool SupportsChildren => true;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            try
            {
                // Function Apps also support file browsing via Kudu
                AddChild(new FilesNode(SubscriptionId, Label));
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

        internal static FunctionAppState ParseState(string state)
        {
            if (string.IsNullOrEmpty(state))
                return FunctionAppState.Unknown;

            if (state.Equals("Running", StringComparison.OrdinalIgnoreCase))
                return FunctionAppState.Running;

            if (state.Equals("Stopped", StringComparison.OrdinalIgnoreCase))
                return FunctionAppState.Stopped;

            return FunctionAppState.Unknown;
        }

        /// <summary>
        /// Determines if a site kind represents a Function App.
        /// </summary>
        internal static bool IsFunctionApp(string kind)
        {
            if (string.IsNullOrEmpty(kind))
                return false;

            // Function Apps have kind containing "functionapp" (e.g., "functionapp", "functionapp,linux")
            return kind.IndexOf("functionapp", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
