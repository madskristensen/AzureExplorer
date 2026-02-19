using System.Diagnostics;
using System.IO;

using AzureExplorer.ToolWindows;
using AzureExplorer.VirtualMachine.Models;
using AzureExplorer.VirtualMachine.Services;

namespace AzureExplorer.VirtualMachine.Commands
{
    [Command(PackageIds.ConnectRdp)]
    internal sealed class ConnectRdpCommand : BaseCommand<ConnectRdpCommand>
    {
        protected override void BeforeQueryStatus(EventArgs e)
        {
            // Only visible for Windows VMs
            Command.Visible = AzureExplorerControl.SelectedNode is VirtualMachineNode node &&
                              node.OsType == VirtualMachineOsType.Windows;
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode is not VirtualMachineNode node)
                return;

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Connecting to {node.Label} via RDP...");

                // Get the public IP address
                var publicIp = node.PublicIpAddress;

                if (string.IsNullOrEmpty(publicIp))
                {
                    publicIp = await VirtualMachineManager.Instance.GetPublicIpAddressAsync(
                        node.SubscriptionId,
                        node.ResourceGroupName,
                        node.Label);

                    if (!string.IsNullOrEmpty(publicIp))
                    {
                        node.UpdatePublicIpAddress(publicIp);
                    }
                }

                if (string.IsNullOrEmpty(publicIp))
                {
                    await VS.MessageBox.ShowWarningAsync(
                        "Connect via RDP",
                        $"No public IP address found for '{node.Label}'.\n\nTo connect via RDP, the VM must have a public IP address assigned.\n\nYou can also connect using a VPN or Azure Bastion from the Azure Portal.");
                    return;
                }

                // Create a temporary .rdp file for better control over connection settings
                var rdpContent = $@"full address:s:{publicIp}
prompt for credentials:i:1
administrative session:i:1";

                var rdpPath = Path.Combine(Path.GetTempPath(), $"{node.Label}.rdp");
                File.WriteAllText(rdpPath, rdpContent);

                // Launch the RDP file (opens with the default RDP client)
                Process.Start(new ProcessStartInfo(rdpPath)
                {
                    UseShellExecute = true
                });

                await VS.StatusBar.ShowMessageAsync($"RDP connection launched for {node.Label}");
            }
            catch (Exception ex)
            {
                await VS.MessageBox.ShowErrorAsync("Connect via RDP", ex.Message);
            }
        }
    }
}
