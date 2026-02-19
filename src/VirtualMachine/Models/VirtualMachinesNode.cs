using System.Collections.Generic;
using System.Linq;
using System.Threading;

using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.VirtualMachine.Models
{
    /// <summary>
    /// Category node that groups Azure Virtual Machines under a resource group.
    /// </summary>
    internal sealed class VirtualMachinesNode : ExplorerNodeBase
    {
        public VirtualMachinesNode(string subscriptionId, string resourceGroupName)
            : base("Virtual Machines")
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }

        public override ImageMoniker IconMoniker => KnownMonikers.AzureVirtualMachine;
        public override int ContextMenuId => PackageIds.VirtualMachinesCategoryContextMenu;
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
                    "Microsoft.Compute/virtualMachines",
                    ResourceGroupName,
                    cancellationToken);

                foreach (ResourceGraphResult resource in resources.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var vmSize = resource.GetProperty("hardwareProfile.vmSize");
                    var osType = resource.GetProperty("storageProfile.osDisk.osType");

                    AddChild(new VirtualMachineNode(
                        resource.Name,
                        SubscriptionId,
                        ResourceGroupName,
                        state: null,
                        vmSize,
                        osType,
                        publicIpAddress: null,
                        privateIpAddress: null,
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
