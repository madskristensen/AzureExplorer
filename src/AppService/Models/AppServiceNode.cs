using System.Threading;

using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.AppService.Models
{
    /// <summary>
    /// Represents an Azure App Service (Web App). Expandable node with Files child and context menu actions.
    /// </summary>
    internal sealed class AppServiceNode(string name, string subscriptionId, string resourceGroupName, string state, string defaultHostName) : WebSiteNodeBase(name, subscriptionId, resourceGroupName, state, defaultHostName)
    {
        protected override ImageMoniker RunningIconMoniker => KnownMonikers.AzureWebSites;

        public override int ContextMenuId => PackageIds.AppServiceContextMenu;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            await LoadChildrenWithErrorHandlingAsync(_ =>
            {
                AddChild(new FilesNode(SubscriptionId, Label));
                return Task.CompletedTask;
            }, cancellationToken);
        }
    }
}
