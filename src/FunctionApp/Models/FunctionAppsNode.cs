using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.Resources;

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
                ArmClient client = AzureResourceService.Instance.GetClient(SubscriptionId);
                SubscriptionResource sub = client.GetSubscriptionResource(
                    SubscriptionResource.CreateResourceIdentifier(SubscriptionId));
                ResourceGroupResource rg = (await sub.GetResourceGroupAsync(ResourceGroupName, cancellationToken)).Value;

                var functionApps = new List<FunctionAppNode>();

                await foreach (WebSiteResource site in rg.GetWebSites().GetAllAsync(cancellationToken: cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Filter to only include Function Apps
                    if (!FunctionAppNode.IsFunctionApp(site.Data.Kind))
                        continue;

                    functionApps.Add(new FunctionAppNode(
                        site.Data.Name,
                        SubscriptionId,
                        ResourceGroupName,
                        site.Data.State,
                        site.Data.DefaultHostName,
                        site.Data.Tags));
                }

                // Sort alphabetically by name
                foreach (FunctionAppNode node in functionApps.OrderBy(f => f.Label, StringComparer.OrdinalIgnoreCase))
                {
                    AddChild(node);
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
