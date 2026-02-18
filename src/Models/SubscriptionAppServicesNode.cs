using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.Resources;

using AzureExplorer.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Models
{
    /// <summary>
    /// Category node that lists all App Services across the entire subscription.
    /// This allows users to see App Services they have direct access to, even if they
    /// don't have list permissions on the containing resource group.
    /// </summary>
    internal sealed class SubscriptionAppServicesNode : ExplorerNodeBase
    {
        public SubscriptionAppServicesNode(string subscriptionId)
            : base("App Services")
        {
            SubscriptionId = subscriptionId;
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }

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

                var appServices = new List<AppServiceNode>();

                await foreach (WebSiteResource site in sub.GetWebSitesAsync(cancellationToken: cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Extract resource group name from the resource ID
                    string resourceGroupName = site.Id.ResourceGroupName;

                    appServices.Add(new AppServiceNode(
                        site.Data.Name,
                        SubscriptionId,
                        resourceGroupName,
                        site.Data.State,
                        site.Data.DefaultHostName));
                }

                // Sort alphabetically by name
                foreach (var node in appServices.OrderBy(a => a.Label, StringComparer.OrdinalIgnoreCase))
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
