using AzureExplorer.AppService.Models;
using AzureExplorer.AppService.Services;
using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.AppService.Commands
{
    [Command(PackageIds.RestartSlot)]
    internal sealed class RestartSlotCommand : BaseCommand<RestartSlotCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode?.ActualNode is not DeploymentSlotNode slot) return;

            var confirmed = await VS.MessageBox.ShowConfirmAsync(
                "Restart Deployment Slot",
                $"Are you sure you want to restart slot '{slot.SlotName}'? This will briefly interrupt the slot.");

            if (!confirmed) return;

            var activity = ActivityLogService.Instance.LogActivity(
                "Restarting",
                $"{slot.AppServiceName}/{slot.SlotName}",
                "DeploymentSlot");

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Restarting slot {slot.SlotName}...");
                await AppServiceManager.Instance.RestartSlotAsync(
                    slot.SubscriptionId,
                    slot.ResourceGroupName,
                    slot.AppServiceName,
                    slot.SlotName);

                slot.State = WebSiteState.Running;

                activity.Complete();
                await VS.StatusBar.ShowMessageAsync($"Slot '{slot.SlotName}' restarted.");
            }
            catch (Exception ex)
            {
                activity.Fail(ex.Message);
                await VS.MessageBox.ShowErrorAsync("Restart Deployment Slot", ex.Message);
            }
        }
    }
}
