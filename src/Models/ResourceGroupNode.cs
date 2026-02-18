using System;
using System.Threading;
using System.Threading.Tasks;

using AzureExplorer.Services;

using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.Resources;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Models
{
    /// <summary>
    /// Represents an Azure resource group. Loads App Service web sites directly
    /// using the App Service SDK (rather than generic resource listing) so that
    /// state and hostname are immediately available.
    /// </summary>
    internal sealed class ResourceGroupNode : ExplorerNodeBase
    {
        public ResourceGroupNode(string name, string subscriptionId) : base(name)
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = name;
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }

        public override ImageMoniker IconMoniker => KnownMonikers.AzureResourceGroup;
        public override int ContextMenuId => PackageIds.ResourceGroupContextMenu;
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

                await foreach (WebSiteResource site in rg.GetWebSites().GetAllAsync(cancellationToken: cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    AddChild(new AppServiceNode(
                        site.Data.Name,
                        SubscriptionId,
                        ResourceGroupName,
                        site.Data.State,
                        site.Data.DefaultHostName));
                }
            }
            catch (Exception ex)
            {
                if (Children.Count <= 1) // only LoadingNode or empty
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
