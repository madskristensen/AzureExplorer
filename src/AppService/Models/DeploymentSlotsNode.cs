using System.Threading;
using System.Threading.Tasks;

using AzureExplorer.AppService.Services;
using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.AppService.Models
{
    /// <summary>
    /// Container node representing deployment slots under an App Service.
    /// Shows all non-production slots when expanded.
    /// </summary>
    internal sealed class DeploymentSlotsNode : ExplorerNodeBase
    {
        public DeploymentSlotsNode(string subscriptionId, string resourceGroupName, string appServiceName)
            : base("Deployment Slots")
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            AppServiceName = appServiceName;

            // Add loading placeholder
            Children.Add(new LoadingNode());
        }

        /// <summary>
        /// Gets the subscription ID containing the App Service.
        /// </summary>
        public string SubscriptionId { get; }

        /// <summary>
        /// Gets the resource group name.
        /// </summary>
        public string ResourceGroupName { get; }

        /// <summary>
        /// Gets the parent App Service name.
        /// </summary>
        public string AppServiceName { get; }

        public override ImageMoniker IconMoniker => KnownMonikers.CloudServer;

        public override int ContextMenuId => 0;

        public override bool SupportsChildren => true;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            await LoadChildrenWithErrorHandlingAsync(async ct =>
            {
                var slots = await AppServiceManager.Instance.GetDeploymentSlotsAsync(
                    SubscriptionId,
                    ResourceGroupName,
                    AppServiceName,
                    ct);

                foreach (var slot in slots)
                {
                    AddChild(slot);
                }

                // Update description with slot count
                Description = slots.Count == 0 ? "No slots" : $"{slots.Count} slot{(slots.Count == 1 ? "" : "s")}";
            }, cancellationToken);
        }
    }
}
