using System;
using System.Threading;
using System.Threading.Tasks;

using AzureExplorer.AppService.Models;
using AzureExplorer.AppServicePlan.Models;
using AzureExplorer.Core.Models;
using AzureExplorer.FrontDoor.Models;
using AzureExplorer.FunctionApp.Models;
using AzureExplorer.KeyVault.Models;
using AzureExplorer.Sql.Models;
using AzureExplorer.Storage.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.ResourceGroup.Models
{
    /// <summary>
    /// Represents an Azure resource group. Contains category nodes for different
    /// resource types (App Services, App Service Plans, etc.).
    /// </summary>
    internal sealed class ResourceGroupNode : ExplorerNodeBase, IPortalResource
    {
        public ResourceGroupNode(string name, string subscriptionId) : base(name)
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = name;
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }

        // IPortalResource - resource groups don't have a provider path
        string IPortalResource.ResourceName => null;
        string IPortalResource.AzureResourceProvider => null;

        public override ImageMoniker IconMoniker => KnownMonikers.AzureResourceGroup;
        public override int ContextMenuId => PackageIds.ResourceGroupContextMenu;
        public override bool SupportsChildren => true;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            // Add category nodes for each resource type
            var appServicesNode = new AppServicesNode(SubscriptionId, ResourceGroupName);
            var appServicePlansNode = new AppServicePlansNode(SubscriptionId, ResourceGroupName);
            var functionAppsNode = new FunctionAppsNode(SubscriptionId, ResourceGroupName);
            var frontDoorsNode = new FrontDoorsNode(SubscriptionId, ResourceGroupName);
            var keyVaultsNode = new KeyVaultsNode(SubscriptionId, ResourceGroupName);
            var sqlServersNode = new SqlServersNode(SubscriptionId, ResourceGroupName);
            var storageAccountsNode = new StorageAccountsNode(SubscriptionId, ResourceGroupName);

            AddChild(appServicesNode);
            AddChild(appServicePlansNode);
            AddChild(frontDoorsNode);
            AddChild(functionAppsNode);
            AddChild(keyVaultsNode);
            AddChild(sqlServersNode);
            AddChild(storageAccountsNode);

            EndLoading();

            // Pre-load category children in parallel (fire-and-forget with error handling)
            _ = PreloadCategoryChildrenAsync(
                appServicesNode, appServicePlansNode, functionAppsNode, frontDoorsNode, 
                keyVaultsNode, sqlServersNode, storageAccountsNode, cancellationToken);
        }

        private static async Task PreloadCategoryChildrenAsync(
            AppServicesNode appServicesNode,
            AppServicePlansNode appServicePlansNode,
            FunctionAppsNode functionAppsNode,
            FrontDoorsNode frontDoorsNode,
            KeyVaultsNode keyVaultsNode,
            SqlServersNode sqlServersNode,
            StorageAccountsNode storageAccountsNode,
            CancellationToken cancellationToken)
        {
            try
            {
                await Task.WhenAll(
                    appServicesNode.LoadChildrenAsync(cancellationToken),
                    appServicePlansNode.LoadChildrenAsync(cancellationToken),
                    functionAppsNode.LoadChildrenAsync(cancellationToken),
                    frontDoorsNode.LoadChildrenAsync(cancellationToken),
                    keyVaultsNode.LoadChildrenAsync(cancellationToken),
                    sqlServersNode.LoadChildrenAsync(cancellationToken),
                    storageAccountsNode.LoadChildrenAsync(cancellationToken));
            }
            catch (OperationCanceledException)
            {
                // Expected when user navigates away; ignore
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Pre-load failed: {ex.Message}");
            }
        }
    }
}
