using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.VirtualMachine.Models
{
    /// <summary>
    /// Category node that lists all Azure Virtual Machines across the entire subscription.
    /// </summary>
    internal sealed class SubscriptionVirtualMachinesNode(string subscriptionId) : SubscriptionResourceNodeBase("Virtual Machines", subscriptionId)
    {
        protected override string ResourceType => "Microsoft.Compute/virtualMachines";

        public override ImageMoniker IconMoniker => KnownMonikers.AzureVirtualMachine;
        public override int ContextMenuId => PackageIds.VirtualMachinesCategoryContextMenu;

        protected override ExplorerNodeBase CreateNodeFromGraphResult(ResourceGraphResult resource)
        {
            var vmSize = resource.GetProperty("hardwareProfile.vmSize");
            var osType = resource.GetProperty("storageProfile.osDisk.osType");

            // Note: Public/private IP requires additional API calls (network interfaces).
            // The VirtualMachineManager will fetch these when needed.
            return new VirtualMachineNode(
                resource.Name,
                SubscriptionId,
                resource.ResourceGroup,
                state: null, // Power state requires instance view, will be fetched by manager
                vmSize,
                osType,
                publicIpAddress: null,
                privateIpAddress: null,
                resource.Tags);
        }
    }
}
