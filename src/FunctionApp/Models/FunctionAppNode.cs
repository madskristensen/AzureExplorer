using System.Threading;

using AzureExplorer.AppService.Models;
using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.FunctionApp.Models
{
    /// <summary>
    /// Represents an Azure Function App. Similar to App Service but with Function-specific icon and context.
    /// </summary>
    internal sealed class FunctionAppNode(string name, string subscriptionId, string resourceGroupName, string state, string defaultHostName) : WebSiteNodeBase(name, subscriptionId, resourceGroupName, state, defaultHostName)
    {
        protected override ImageMoniker RunningIconMoniker => KnownMonikers.AzureFunctionsApp;

        public override int ContextMenuId => PackageIds.FunctionAppContextMenu;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            await LoadChildrenWithErrorHandlingAsync(_ =>
            {
                // Function Apps also support file browsing via Kudu
                AddChild(new FilesNode(SubscriptionId, Label));
                return Task.CompletedTask;
            }, cancellationToken);
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
