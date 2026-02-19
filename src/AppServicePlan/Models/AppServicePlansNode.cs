using System;
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

namespace AzureExplorer.AppServicePlan.Models
{
    /// <summary>
    /// Category node that groups App Service Plans under a resource group.
    /// </summary>
    internal sealed class AppServicePlansNode : ExplorerNodeBase
    {
        public AppServicePlansNode(string subscriptionId, string resourceGroupName)
            : base("App Service Plans")
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }

        public override ImageMoniker IconMoniker => KnownMonikers.CloudServer;
        public override int ContextMenuId => PackageIds.AppServicePlansCategoryContextMenu;
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

                var plans = new List<AppServicePlanNode>();

                await foreach (AppServicePlanResource plan in rg.GetAppServicePlans().GetAllAsync(cancellationToken: cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    plans.Add(new AppServicePlanNode(
                        plan.Data.Name,
                        SubscriptionId,
                        ResourceGroupName,
                        plan.Data.Sku?.Name,
                        plan.Data.Kind,
                        plan.Data.NumberOfSites));
                }

                // Sort alphabetically by name
                foreach (AppServicePlanNode node in plans.OrderBy(p => p.Label, StringComparer.OrdinalIgnoreCase))
                {
                    AddChild(node);
                }
            }
            catch (Exception ex)
            {
                if (Children.Count <= 1)
                {
                    Children.Clear();
                    Children.Add(new LoadingNode { Label = $"Error: {ex.Message}" });
                    IsLoading = false;
                    IsLoaded = true;
                    return;
                }
            }

            EndLoading();
        }
    }
}
