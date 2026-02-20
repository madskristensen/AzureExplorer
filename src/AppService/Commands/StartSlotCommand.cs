using AzureExplorer.AppService.Models;
using AzureExplorer.AppService.Services;
using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.AppService.Commands
{
    [Command(PackageIds.StartSlot)]
    internal sealed class StartSlotCommand : BaseCommand<StartSlotCommand>
    {
        protected override void BeforeQueryStatus(EventArgs e)
        {
            Command.Enabled = AzureExplorerControl.SelectedNode?.ActualNode is DeploymentSlotNode slot
                && slot.State == WebSiteState.Stopped;
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode?.ActualNode is not DeploymentSlotNode slot) return;

            var activity = ActivityLogService.Instance.LogActivity(
                "Starting",
                $"{slot.AppServiceName}/{slot.SlotName}",
                "DeploymentSlot");

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Starting slot {slot.SlotName}...");
                await AppServiceManager.Instance.StartSlotAsync(
                    slot.SubscriptionId,
                    slot.ResourceGroupName,
                    slot.AppServiceName,
                    slot.SlotName);

                slot.State = WebSiteState.Running;

                activity.Complete();
                await VS.StatusBar.ShowMessageAsync($"Slot '{slot.SlotName}' started.");
            }
            catch (Exception ex)
            {
                activity.Fail(ex.Message);
                await VS.MessageBox.ShowErrorAsync("Start Deployment Slot", ex.Message);
            }
        }
    }
}
