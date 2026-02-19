using System.Text.Json;

using Azure.ResourceManager.Resources;

using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.VirtualMachine.Models
{
    /// <summary>
    /// Category node that lists all Azure Virtual Machines across the entire subscription.
    /// </summary>
    internal sealed class SubscriptionVirtualMachinesNode : SubscriptionResourceNodeBase
    {
        public SubscriptionVirtualMachinesNode(string subscriptionId)
            : base("Virtual Machines", subscriptionId)
        {
        }

        protected override string ResourceType => "Microsoft.Compute/virtualMachines";

        public override ImageMoniker IconMoniker => KnownMonikers.AzureVirtualMachine;
        public override int ContextMenuId => PackageIds.VirtualMachinesCategoryContextMenu;

        protected override ExplorerNodeBase CreateNodeFromResource(string name, string resourceGroup, GenericResource resource)
        {
            string vmSize = null;
            string osType = null;

            if (resource.Data.Properties != null)
            {
                try
                {
                    using var doc = JsonDocument.Parse(resource.Data.Properties);
                    JsonElement root = doc.RootElement;

                    // Get VM size from hardwareProfile
                    if (root.TryGetProperty("hardwareProfile", out JsonElement hardwareProfile) &&
                        hardwareProfile.TryGetProperty("vmSize", out JsonElement sizeElement))
                    {
                        vmSize = sizeElement.GetString();
                    }

                    // Get OS type from storageProfile.osDisk.osType
                    if (root.TryGetProperty("storageProfile", out JsonElement storageProfile) &&
                        storageProfile.TryGetProperty("osDisk", out JsonElement osDisk) &&
                        osDisk.TryGetProperty("osType", out JsonElement osTypeElement))
                    {
                        osType = osTypeElement.GetString();
                    }
                }
                catch
                {
                    // Properties parsing failed; continue with null values
                }
            }

            // Note: Public/private IP requires additional API calls (network interfaces).
            // The VirtualMachineManager will fetch these when needed.
            return new VirtualMachineNode(
                name,
                SubscriptionId,
                resourceGroup,
                state: null, // Power state requires instance view, will be fetched by manager
                vmSize,
                osType,
                publicIpAddress: null,
                privateIpAddress: null);
        }
    }
}
