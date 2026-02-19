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
                ArmClient client = AzureResourceService.Instance.GetClient(SubscriptionId);
                SubscriptionResource sub = client.GetSubscriptionResource(
                    SubscriptionResource.CreateResourceIdentifier(SubscriptionId));
                ResourceGroupResource rg = (await sub.GetResourceGroupAsync(ResourceGroupName, cancellationToken)).Value;

                var appServices = new List<AppServiceNode>();

                await foreach (WebSiteResource site in rg.GetWebSites().GetAllAsync(cancellationToken: cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    appServices.Add(new AppServiceNode(
                        site.Data.Name,
                        SubscriptionId,
                        ResourceGroupName,
                        site.Data.State,
                        site.Data.DefaultHostName));
                }

                // Sort alphabetically by name
                foreach (AppServiceNode node in appServices.OrderBy(a => a.Label, StringComparer.OrdinalIgnoreCase))
                {
                    AddChild(node);
                }
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
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
