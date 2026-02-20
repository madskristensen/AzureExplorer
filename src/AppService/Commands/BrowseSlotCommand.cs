using AzureExplorer.AppService.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.AppService.Commands
{
    [Command(PackageIds.BrowseSlot)]
    internal sealed class BrowseSlotCommand : BaseCommand<BrowseSlotCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode?.ActualNode is not DeploymentSlotNode slot) return;

            if (string.IsNullOrEmpty(slot.BrowseUrl))
            {
                await VS.MessageBox.ShowWarningAsync("Browse Slot", "No URL available for this deployment slot.");
                return;
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = slot.BrowseUrl,
                UseShellExecute = true
            });
        }
    }
}
