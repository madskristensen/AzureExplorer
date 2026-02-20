using AzureExplorer.Core.Services;
using AzureExplorer.ToolWindows;
using AzureExplorer.VirtualMachine.Models;
using AzureExplorer.VirtualMachine.Services;

namespace AzureExplorer.VirtualMachine.Commands
{
    [Command(PackageIds.StopVm)]
    internal sealed class StopVmCommand : BaseCommand<StopVmCommand>
    {
        protected override void BeforeQueryStatus(EventArgs e)
        {
            // Only visible when the selected VM is running
            Command.Visible = AzureExplorerControl.SelectedNode?.ActualNode is VirtualMachineNode node &&
                              (node.State == VirtualMachineState.Running ||
                               node.State == VirtualMachineState.Starting);
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode?.ActualNode is not VirtualMachineNode node)
                return;

            // Confirm before stopping (deallocating stops billing)
            var confirmed = await VS.MessageBox.ShowConfirmAsync(
                "Stop Virtual Machine",
                $"Are you sure you want to stop and deallocate '{node.Label}'?\n\nThis will stop any running workloads and release compute resources.");

            if (!confirmed)
                return;

            // Log the activity as in-progress
            var activity = ActivityLogService.Instance.LogActivity(
                "Stopping",
                node.Label,
                "Virtual Machine");

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Stopping {node.Label}...");
                node.State = VirtualMachineState.Deallocating;

                await VirtualMachineManager.Instance.StopAsync(
                    node.SubscriptionId,
                    node.ResourceGroupName,
                    node.Label,
                    deallocate: true);

                node.State = VirtualMachineState.Deallocated;
                activity.Complete();
                await VS.StatusBar.ShowMessageAsync($"{node.Label} stopped and deallocated.");
            }
            catch (Exception ex)
            {
                node.State = VirtualMachineState.Unknown;
                activity.Fail(ex.Message);
                await VS.MessageBox.ShowErrorAsync("Stop Virtual Machine", ex.Message);
            }
        }
    }
}
