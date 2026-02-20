using AzureExplorer.AppService.Models;
using AzureExplorer.AppService.Services;
using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.AppService.Commands
{
    [Command(PackageIds.StopSlot)]
    internal sealed class StopSlotCommand : BaseCommand<StopSlotCommand>
    {
        protected override void BeforeQueryStatus(EventArgs e)
        {
            Command.Enabled = AzureExplorerControl.SelectedNode?.ActualNode is DeploymentSlotNode slot
                && slot.State == WebSiteState.Running;
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode?.ActualNode is not DeploymentSlotNode slot) return;

            var confirmed = await VS.MessageBox.ShowConfirmAsync(
                "Stop Deployment Slot",
                $"Are you sure you want to stop slot '{slot.SlotName}'?");

            if (!confirmed) return;

            var activity = ActivityLogService.Instance.LogActivity(
                "Stopping",
                $"{slot.AppServiceName}/{slot.SlotName}",
                "DeploymentSlot");

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Stopping slot {slot.SlotName}...");
                await AppServiceManager.Instance.StopSlotAsync(
                    slot.SubscriptionId,
                    slot.ResourceGroupName,
                    slot.AppServiceName,
                    slot.SlotName);

                slot.State = WebSiteState.Stopped;

                activity.Complete();
                await VS.StatusBar.ShowMessageAsync($"Slot '{slot.SlotName}' stopped.");
            }
            catch (Exception ex)
            {
                activity.Fail(ex.Message);
                await VS.MessageBox.ShowErrorAsync("Stop Deployment Slot", ex.Message);
            }
        }
    }
}
