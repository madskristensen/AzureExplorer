using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.VirtualMachine.Models
{
    internal enum VirtualMachineState
    {
        Unknown,
        Running,
        Starting,
        Stopping,
        Stopped,
        Deallocating,
        Deallocated
    }

    internal enum VirtualMachineOsType
    {
        Unknown,
        Windows,
        Linux
    }

    /// <summary>
    /// Represents an Azure Virtual Machine in the explorer tree.
    /// </summary>
    internal sealed class VirtualMachineNode : ExplorerNodeBase, IPortalResource
    {
        private VirtualMachineState _state;

        public VirtualMachineNode(
            string name,
            string subscriptionId,
            string resourceGroupName,
            string state,
            string vmSize,
            string osType,
            string publicIpAddress,
            string privateIpAddress)
            : base(name)
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            VmSize = vmSize;
            OsType = ParseOsType(osType);
            PublicIpAddress = publicIpAddress;
            PrivateIpAddress = privateIpAddress;
            _state = ParseState(state);

            UpdateDescription();
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }
        public string VmSize { get; }
        public VirtualMachineOsType OsType { get; }
        public string PublicIpAddress { get; private set; }
        public string PrivateIpAddress { get; }

        // IPortalResource
        public string ResourceName => Label;
        public string AzureResourceProvider => "Microsoft.Compute/virtualMachines";

        public VirtualMachineState State
        {
            get => _state;
            set
            {
                if (SetProperty(ref _state, value))
                {
                    UpdateDescription();
                    OnPropertyChanged(nameof(IconMoniker));
                }
            }
        }

        /// <summary>
        /// Whether this VM can be connected via RDP (Windows VMs with public IP).
        /// </summary>
        public bool CanConnectRdp => OsType == VirtualMachineOsType.Windows && !string.IsNullOrEmpty(PublicIpAddress);

        /// <summary>
        /// Whether this VM can be connected via SSH (Linux VMs with public IP).
        /// </summary>
        public bool CanConnectSsh => OsType == VirtualMachineOsType.Linux && !string.IsNullOrEmpty(PublicIpAddress);

        public override ImageMoniker IconMoniker => State switch
        {
            VirtualMachineState.Running => KnownMonikers.AzureVirtualMachine,
            VirtualMachineState.Starting => KnownMonikers.AzureVirtualMachine,
            VirtualMachineState.Stopped => KnownMonikers.StatusStopped,
            VirtualMachineState.Stopping => KnownMonikers.StatusStopped,
            VirtualMachineState.Deallocated => KnownMonikers.StatusStopped,
            VirtualMachineState.Deallocating => KnownMonikers.StatusStopped,
            _ => KnownMonikers.AzureVirtualMachine
        };

        public override int ContextMenuId => PackageIds.VirtualMachineContextMenu;
        public override bool SupportsChildren => false;

        /// <summary>
        /// Updates the public IP address after fetching from Azure.
        /// </summary>
        public void UpdatePublicIpAddress(string publicIpAddress)
        {
            PublicIpAddress = publicIpAddress;
            OnPropertyChanged(nameof(PublicIpAddress));
            OnPropertyChanged(nameof(CanConnectRdp));
            OnPropertyChanged(nameof(CanConnectSsh));
        }

        private void UpdateDescription()
        {
            string stateText = _state switch
            {
                VirtualMachineState.Running => "Running",
                VirtualMachineState.Starting => "Starting...",
                VirtualMachineState.Stopping => "Stopping...",
                VirtualMachineState.Stopped => "Stopped",
                VirtualMachineState.Deallocating => "Deallocating...",
                VirtualMachineState.Deallocated => "Deallocated",
                _ => ""
            };

            string osText = OsType switch
            {
                VirtualMachineOsType.Windows => "Windows",
                VirtualMachineOsType.Linux => "Linux",
                _ => ""
            };

            Description = !string.IsNullOrEmpty(VmSize)
                ? $"{stateText} • {osText} • {VmSize}"
                : stateText;
        }

        internal static VirtualMachineState ParseState(string state)
        {
            if (string.IsNullOrEmpty(state))
                return VirtualMachineState.Unknown;

            // Azure returns power state as "PowerState/running", "PowerState/deallocated", etc.
            string normalizedState = state.StartsWith("PowerState/", StringComparison.OrdinalIgnoreCase)
                ? state.Substring("PowerState/".Length)
                : state;

            if (normalizedState.Equals("running", StringComparison.OrdinalIgnoreCase))
                return VirtualMachineState.Running;

            if (normalizedState.Equals("starting", StringComparison.OrdinalIgnoreCase))
                return VirtualMachineState.Starting;

            if (normalizedState.Equals("stopping", StringComparison.OrdinalIgnoreCase))
                return VirtualMachineState.Stopping;

            if (normalizedState.Equals("stopped", StringComparison.OrdinalIgnoreCase))
                return VirtualMachineState.Stopped;

            if (normalizedState.Equals("deallocating", StringComparison.OrdinalIgnoreCase))
                return VirtualMachineState.Deallocating;

            if (normalizedState.Equals("deallocated", StringComparison.OrdinalIgnoreCase))
                return VirtualMachineState.Deallocated;

            return VirtualMachineState.Unknown;
        }

        internal static VirtualMachineOsType ParseOsType(string osType)
        {
            if (string.IsNullOrEmpty(osType))
                return VirtualMachineOsType.Unknown;

            if (osType.Equals("Windows", StringComparison.OrdinalIgnoreCase))
                return VirtualMachineOsType.Windows;

            if (osType.Equals("Linux", StringComparison.OrdinalIgnoreCase))
                return VirtualMachineOsType.Linux;

            return VirtualMachineOsType.Unknown;
        }
    }
}
