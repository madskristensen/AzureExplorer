using System.Collections.Generic;
using System.Linq;
using System.Threading;

using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;
using AzureExplorer.FunctionApp.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.AppService.Models
{
    /// <summary>
    /// Category node that groups App Services (Web Apps) under a resource group.
    /// </summary>
    internal sealed class AppServicesNode : ExplorerNodeBase
    {
        public AppServicesNode(string subscriptionId, string resourceGroupName)
            : base("App Services")
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }

        public override ImageMoniker IconMoniker => KnownMonikers.Web;
        public override int ContextMenuId => PackageIds.AppServicesCategoryContextMenu;
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

                    // Skip Function Apps (they have their own category)
                    if (FunctionAppNode.IsFunctionApp(resource.Kind))
                        continue;

                    var state = resource.GetProperty("state");
                    var defaultHostName = resource.GetProperty("defaultHostName");

                    AddChild(new AppServiceNode(
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

            EndLoading();
        }
    }
}
