using AzureExplorer.Core.Services;
using AzureExplorer.ToolWindows;
using AzureExplorer.VirtualMachine.Models;
using AzureExplorer.VirtualMachine.Services;

namespace AzureExplorer.VirtualMachine.Commands
{
    [Command(PackageIds.StartVm)]
    internal sealed class StartVmCommand : BaseCommand<StartVmCommand>
    {
        protected override void BeforeQueryStatus(EventArgs e)
        {
            // Only visible when the selected VM is not running
            Command.Visible = AzureExplorerControl.SelectedNode?.ActualNode is VirtualMachineNode node &&
                              node.State != VirtualMachineState.Running &&
                              node.State != VirtualMachineState.Starting;
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode?.ActualNode is not VirtualMachineNode node)
                return;

            // Log the activity as in-progress
            var activity = ActivityLogService.Instance.LogActivity(
                "Starting",
                node.Label,
                "Virtual Machine");

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Starting {node.Label}...");
                node.State = VirtualMachineState.Starting;

                await VirtualMachineManager.Instance.StartAsync(
                    node.SubscriptionId,
                    node.ResourceGroupName,
                    node.Label);

                node.State = VirtualMachineState.Running;
                activity.Complete();
                await VS.StatusBar.ShowMessageAsync($"{node.Label} started.");
            }
            catch (Exception ex)
            {
                node.State = VirtualMachineState.Unknown;
                activity.Fail(ex.Message);
                await VS.MessageBox.ShowErrorAsync("Start Virtual Machine", ex.Message);
            }
        }
    }
}
