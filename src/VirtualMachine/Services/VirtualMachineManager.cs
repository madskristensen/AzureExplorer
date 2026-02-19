using System;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Resources;

using AzureExplorer.Core.Services;

namespace AzureExplorer.VirtualMachine.Services
{
    /// <summary>
    /// Provides Virtual Machine specific operations: start, stop, restart, and state queries.
    /// </summary>
    internal sealed class VirtualMachineManager
    {
        private static readonly Lazy<VirtualMachineManager> _instance = new(() => new VirtualMachineManager());

        private VirtualMachineManager() { }

        public static VirtualMachineManager Instance => _instance.Value;

        public async Task StartAsync(string subscriptionId, string resourceGroupName, string name, CancellationToken cancellationToken = default)
        {
            VirtualMachineResource vm = await GetVirtualMachineAsync(subscriptionId, resourceGroupName, name, cancellationToken);
            await vm.PowerOnAsync(WaitUntil.Started, cancellationToken);
        }

        public async Task StopAsync(string subscriptionId, string resourceGroupName, string name, bool deallocate = true, CancellationToken cancellationToken = default)
        {
            VirtualMachineResource vm = await GetVirtualMachineAsync(subscriptionId, resourceGroupName, name, cancellationToken);

            if (deallocate)
            {
                // Deallocate stops the VM and releases compute resources (no charges)
                await vm.DeallocateAsync(WaitUntil.Started, cancellationToken: cancellationToken);
            }
            else
            {
                // Power off keeps the VM allocated (charges continue)
                await vm.PowerOffAsync(WaitUntil.Started, cancellationToken: cancellationToken);
            }
        }

        public async Task RestartAsync(string subscriptionId, string resourceGroupName, string name, CancellationToken cancellationToken = default)
        {
            VirtualMachineResource vm = await GetVirtualMachineAsync(subscriptionId, resourceGroupName, name, cancellationToken);
            await vm.RestartAsync(WaitUntil.Started, cancellationToken);
        }

        /// <summary>
        /// Gets the power state of the VM (requires instance view).
        /// </summary>
        public async Task<string> GetPowerStateAsync(string subscriptionId, string resourceGroupName, string name, CancellationToken cancellationToken = default)
        {
            VirtualMachineResource vm = await GetVirtualMachineAsync(subscriptionId, resourceGroupName, name, cancellationToken);

            // Get instance view which contains power state
            Response<VirtualMachineResource> response = await vm.GetAsync(cancellationToken: cancellationToken);
            VirtualMachineResource vmWithDetails = response.Value;

            // The instance view requires an explicit call
            VirtualMachineInstanceView instanceView = (await vm.InstanceViewAsync(cancellationToken)).Value;

            foreach (InstanceViewStatus status in instanceView.Statuses)
            {
                if (status.Code != null && status.Code.StartsWith("PowerState/", StringComparison.OrdinalIgnoreCase))
                {
                    return status.Code;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the public IP address of the VM's primary network interface.
        /// Returns null if the VM has no public IP configured.
        /// </summary>
        public async Task<string> GetPublicIpAddressAsync(string subscriptionId, string resourceGroupName, string name, CancellationToken cancellationToken = default)
        {
            VirtualMachineResource vm = await GetVirtualMachineAsync(subscriptionId, resourceGroupName, name, cancellationToken);

            // Get the VM's network profile
            VirtualMachineData vmData = vm.Data;
            if (vmData.NetworkProfile?.NetworkInterfaces == null || vmData.NetworkProfile.NetworkInterfaces.Count == 0)
                return null;

            // Get the primary network interface
            VirtualMachineNetworkInterfaceReference primaryNic = null;
            foreach (VirtualMachineNetworkInterfaceReference nic in vmData.NetworkProfile.NetworkInterfaces)
            {
                if (nic.Primary == true || vmData.NetworkProfile.NetworkInterfaces.Count == 1)
                {
                    primaryNic = nic;
                    break;
                }
            }

            if (primaryNic?.Id == null)
                return null;

            // Get the network interface resource
            ArmClient client = AzureResourceService.Instance.GetClient(subscriptionId);
            NetworkInterfaceResource nicResource = client.GetNetworkInterfaceResource(primaryNic.Id);
            nicResource = (await nicResource.GetAsync(cancellationToken: cancellationToken)).Value;

            // Find the primary IP configuration with a public IP
            foreach (NetworkInterfaceIPConfigurationData ipConfig in nicResource.Data.IPConfigurations)
            {
                if (ipConfig.PublicIPAddress?.Id != null)
                {
                    // Get the public IP resource
                    PublicIPAddressResource publicIpResource = client.GetPublicIPAddressResource(ipConfig.PublicIPAddress.Id);
                    publicIpResource = (await publicIpResource.GetAsync(cancellationToken: cancellationToken)).Value;

                    return publicIpResource.Data.IPAddress;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the private IP address of the VM's primary network interface.
        /// </summary>
        public async Task<string> GetPrivateIpAddressAsync(string subscriptionId, string resourceGroupName, string name, CancellationToken cancellationToken = default)
        {
            VirtualMachineResource vm = await GetVirtualMachineAsync(subscriptionId, resourceGroupName, name, cancellationToken);

            VirtualMachineData vmData = vm.Data;
            if (vmData.NetworkProfile?.NetworkInterfaces == null || vmData.NetworkProfile.NetworkInterfaces.Count == 0)
                return null;

            // Get the primary network interface
            VirtualMachineNetworkInterfaceReference primaryNic = null;
            foreach (VirtualMachineNetworkInterfaceReference nic in vmData.NetworkProfile.NetworkInterfaces)
            {
                if (nic.Primary == true || vmData.NetworkProfile.NetworkInterfaces.Count == 1)
                {
                    primaryNic = nic;
                    break;
                }
            }

            if (primaryNic?.Id == null)
                return null;

            ArmClient client = AzureResourceService.Instance.GetClient(subscriptionId);
            NetworkInterfaceResource nicResource = client.GetNetworkInterfaceResource(primaryNic.Id);
            nicResource = (await nicResource.GetAsync(cancellationToken: cancellationToken)).Value;

            // Find the primary IP configuration
            foreach (NetworkInterfaceIPConfigurationData ipConfig in nicResource.Data.IPConfigurations)
            {
                if (ipConfig.Primary == true || nicResource.Data.IPConfigurations.Count == 1)
                {
                    return ipConfig.PrivateIPAddress;
                }
            }

            return null;
        }

        private async Task<VirtualMachineResource> GetVirtualMachineAsync(string subscriptionId, string resourceGroupName, string name, CancellationToken cancellationToken)
        {
            ArmClient client = AzureResourceService.Instance.GetClient(subscriptionId);
            SubscriptionResource sub = client.GetSubscriptionResource(
                SubscriptionResource.CreateResourceIdentifier(subscriptionId));
            ResourceGroupResource rg = (await sub.GetResourceGroupAsync(resourceGroupName, cancellationToken)).Value;
            VirtualMachineResource vm = (await rg.GetVirtualMachineAsync(name, cancellationToken: cancellationToken)).Value;
            return vm;
        }
    }
}
