using AzureExplorer.AppService.Models;
using AzureExplorer.AppService.Services;
using AzureExplorer.Core.Services;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.AppService.Commands
{
    [Command(PackageIds.SwapSlot)]
    internal sealed class SwapSlotCommand : BaseCommand<SwapSlotCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode?.ActualNode is not DeploymentSlotNode slot) return;

            var confirmed = await VS.MessageBox.ShowConfirmAsync(
                "Swap Deployment Slot",
                $"Are you sure you want to swap '{slot.SlotName}' with production?\n\n" +
                $"This will make the content of '{slot.SlotName}' live in production.");

            if (!confirmed) return;

            var activity = ActivityLogService.Instance.LogActivity(
                "Swapping",
                $"{slot.AppServiceName}/{slot.SlotName}",
                "DeploymentSlot");

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Swapping {slot.SlotName} with production...");
                await AppServiceManager.Instance.SwapSlotAsync(
                    slot.SubscriptionId,
                    slot.ResourceGroupName,
                    slot.AppServiceName,
                    slot.SlotName);

                activity.Complete();
                await VS.StatusBar.ShowMessageAsync($"Slot '{slot.SlotName}' swapped with production.");
            }
            catch (Exception ex)
            {
                activity.Fail(ex.Message);
                await VS.MessageBox.ShowErrorAsync("Swap Deployment Slot", ex.Message);
            }
        }
    }
}
