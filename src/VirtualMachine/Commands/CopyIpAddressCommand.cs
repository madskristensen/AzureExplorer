using System.Windows;

using AzureExplorer.ToolWindows;
using AzureExplorer.VirtualMachine.Models;
using AzureExplorer.VirtualMachine.Services;

namespace AzureExplorer.VirtualMachine.Commands
{
    [Command(PackageIds.CopyVmIpAddress)]
    internal sealed class CopyIpAddressCommand : BaseCommand<CopyIpAddressCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode is not VirtualMachineNode node)
                return;

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Fetching IP address for {node.Label}...");

                // Try to get public IP first, fall back to private IP
                string ipAddress = await VirtualMachineManager.Instance.GetPublicIpAddressAsync(
                    node.SubscriptionId,
                    node.ResourceGroupName,
                    node.Label);

                string ipType = "Public";

                if (string.IsNullOrEmpty(ipAddress))
                {
                    ipAddress = await VirtualMachineManager.Instance.GetPrivateIpAddressAsync(
                        node.SubscriptionId,
                        node.ResourceGroupName,
                        node.Label);
                    ipType = "Private";
                }

                if (string.IsNullOrEmpty(ipAddress))
                {
                    await VS.MessageBox.ShowWarningAsync(
                        "Copy IP Address",
                        $"No IP address found for '{node.Label}'.\n\nThe VM may not have a network interface configured.");
                    return;
                }

                // Update the node with the fetched IP
                if (ipType == "Public")
                {
                    node.UpdatePublicIpAddress(ipAddress);
                }

                Clipboard.SetText(ipAddress);
                await VS.StatusBar.ShowMessageAsync($"{ipType} IP address copied: {ipAddress}");
            }
            catch (Exception ex)
            {
                await VS.MessageBox.ShowErrorAsync("Copy IP Address", ex.Message);
            }
        }
    }
}
