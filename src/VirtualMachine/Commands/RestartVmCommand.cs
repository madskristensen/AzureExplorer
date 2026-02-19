using AzureExplorer.ToolWindows;
using AzureExplorer.VirtualMachine.Models;
using AzureExplorer.VirtualMachine.Services;

namespace AzureExplorer.VirtualMachine.Commands
{
    [Command(PackageIds.RestartVm)]
    internal sealed class RestartVmCommand : BaseCommand<RestartVmCommand>
    {
        protected override void BeforeQueryStatus(EventArgs e)
        {
            // Only visible when the selected VM is running
            Command.Visible = AzureExplorerControl.SelectedNode?.ActualNode is VirtualMachineNode node &&
                              node.State == VirtualMachineState.Running;
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode?.ActualNode is not VirtualMachineNode node)
                return;

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Restarting {node.Label}...");

                await VirtualMachineManager.Instance.RestartAsync(
                    node.SubscriptionId,
                    node.ResourceGroupName,
                    node.Label);

                node.State = VirtualMachineState.Running;
                await VS.StatusBar.ShowMessageAsync($"{node.Label} restarted.");
            }
            catch (Exception ex)
            {
                await VS.MessageBox.ShowErrorAsync("Restart Virtual Machine", ex.Message);
            }
        }
    }
}
