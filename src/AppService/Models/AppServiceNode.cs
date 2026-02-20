using System.Collections.Generic;
using System.Threading;

using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.AppService.Models
{
    /// <summary>
    /// Represents an Azure App Service (Web App). Expandable node with Files child and context menu actions.
    /// </summary>
    internal sealed class AppServiceNode(string name, string subscriptionId, string resourceGroupName, string state, string defaultHostName, IDictionary<string, string> tags = null) : WebSiteNodeBase(name, subscriptionId, resourceGroupName, state, defaultHostName, tags)
    {
        protected override ImageMoniker RunningIconMoniker => KnownMonikers.AzureWebSites;

        public override int ContextMenuId => PackageIds.AppServiceContextMenu;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            await LoadChildrenWithErrorHandlingAsync(_ =>
            {
                // Add Deployment Slots node for managing staging/preview slots
                AddChild(new DeploymentSlotsNode(SubscriptionId, ResourceGroupName, Label));

                // Add Files node for browsing wwwroot contents
                AddChild(new FilesNode(SubscriptionId, Label));

                // Add Tags node if resource has tags
                if (Tags.Count > 0)
                {
                    AddChild(new TagsNode(Tags));
                }

                return Task.CompletedTask;
            }, cancellationToken);
        }
    }
}
