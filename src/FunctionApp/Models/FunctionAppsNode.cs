using System.Collections.Generic;
using System.Linq;
using System.Threading;

using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.FunctionApp.Models
{
    /// <summary>
    /// Category node that groups Function Apps under a resource group.
    /// Filters Microsoft.Web/sites by kind containing "functionapp".
    /// </summary>
    internal sealed class FunctionAppsNode : ExplorerNodeBase
    {
        public FunctionAppsNode(string subscriptionId, string resourceGroupName)
            : base("Function Apps")
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }

        public override ImageMoniker IconMoniker => KnownMonikers.AzureFunctionsApp;
        public override int ContextMenuId => PackageIds.FunctionAppsCategoryContextMenu;
        public override bool SupportsChildren => true;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            try
            {
                // Use Azure Resource Graph for fast loading
                IReadOnlyList<ResourceGraphResult> resources = await ResourceGraphService.Instance.QueryByTypeAsync(
                    SubscriptionId,
                    "Microsoft.Web/sites",
                    ResourceGroupName,
                    cancellationToken);

                foreach (ResourceGraphResult resource in resources.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Filter to only include Function Apps
                    if (!FunctionAppNode.IsFunctionApp(resource.Kind))
                        continue;

                    var state = resource.GetProperty("state");
                    var defaultHostName = resource.GetProperty("defaultHostName");

                    AddChild(new FunctionAppNode(
                        resource.Name,
                        SubscriptionId,
                        ResourceGroupName,
                        state,
                        defaultHostName,
                        resource.Tags));
                }
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
    }
}
