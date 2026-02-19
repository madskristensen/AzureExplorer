using System.Diagnostics;

using AzureExplorer.ToolWindows;
using AzureExplorer.VirtualMachine.Models;
using AzureExplorer.VirtualMachine.Services;

namespace AzureExplorer.VirtualMachine.Commands
{
    [Command(PackageIds.ConnectSsh)]
    internal sealed class ConnectSshCommand : BaseCommand<ConnectSshCommand>
    {
        protected override void BeforeQueryStatus(EventArgs e)
        {
            // Only visible for Linux VMs
            Command.Visible = AzureExplorerControl.SelectedNode is VirtualMachineNode node &&
                              node.OsType == VirtualMachineOsType.Linux;
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode is not VirtualMachineNode node)
                return;

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Connecting to {node.Label} via SSH...");

                // Get the public IP address
                string publicIp = node.PublicIpAddress;

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
                        "Connect via SSH",
                        $"No public IP address found for '{node.Label}'.\n\nTo connect via SSH, the VM must have a public IP address assigned.\n\nYou can also connect using a VPN or Azure Bastion from the Azure Portal.");
                    return;
                }

                // Try to launch Windows Terminal first, fall back to cmd with ssh
                bool launched = TryLaunchWindowsTerminal(publicIp) ||
                                TryLaunchSshInCmd(publicIp);

                if (launched)
                {
                    await VS.StatusBar.ShowMessageAsync($"SSH connection launched for {node.Label}");
                }
                else
                {
                    await VS.MessageBox.ShowWarningAsync(
                        "Connect via SSH",
                        $"Could not launch SSH client.\n\nPlease ensure OpenSSH is installed on your system.\n\nYou can connect manually using:\nssh {publicIp}");
                }
            }
            catch (Exception ex)
            {
                await VS.MessageBox.ShowErrorAsync("Connect via SSH", ex.Message);
            }
        }

        private static bool TryLaunchWindowsTerminal(string ipAddress)
        {
            try
            {
                // Try to launch Windows Terminal with SSH
                var psi = new ProcessStartInfo
                {
                    FileName = "wt.exe",
                    Arguments = $"ssh {ipAddress}",
                    UseShellExecute = true
                };
                Process.Start(psi);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryLaunchSshInCmd(string ipAddress)
        {
            try
            {
                // Fall back to cmd.exe with ssh command
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/k ssh {ipAddress}",
                    UseShellExecute = true
                };
                Process.Start(psi);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
