using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.FunctionApp.Models
{
    /// <summary>
    /// Category node that lists all Function Apps across the entire subscription.
    /// Filters Microsoft.Web/sites by kind containing "functionapp".
    /// </summary>
    internal sealed class SubscriptionFunctionAppsNode(string subscriptionId) : SubscriptionResourceNodeBase("Function Apps", subscriptionId)
    {
        protected override string ResourceType => "Microsoft.Web/sites";

        public override ImageMoniker IconMoniker => KnownMonikers.AzureFunctionsApp;
        public override int ContextMenuId => PackageIds.FunctionAppsCategoryContextMenu;

        protected override bool ShouldIncludeResource(ResourceGraphResult resource)
        {
            // Filter to only include Function Apps (kind contains "functionapp")
            return FunctionAppNode.IsFunctionApp(resource.Kind);
        }

        protected override ExplorerNodeBase CreateNodeFromGraphResult(ResourceGraphResult resource)
        {
            var state = resource.GetProperty("state");
            var defaultHostName = resource.GetProperty("defaultHostName");

            return new FunctionAppNode(
                resource.Name,
                SubscriptionId,
                resource.ResourceGroup,
                state,
                defaultHostName,
                resource.Tags);
        }
    }
}
