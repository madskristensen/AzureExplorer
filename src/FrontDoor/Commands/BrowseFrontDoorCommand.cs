using System.Diagnostics;

using AzureExplorer.FrontDoor.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.FrontDoor.Commands
{
    [Command(PackageIds.BrowseFrontDoor)]
    internal sealed class BrowseFrontDoorCommand : BaseCommand<BrowseFrontDoorCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode is not FrontDoorNode node) return;

            if (string.IsNullOrEmpty(node.BrowseUrl))
            {
                await VS.MessageBox.ShowWarningAsync("No endpoint URL available for this Front Door profile.");
                return;
            }

            Process.Start(node.BrowseUrl);

            await Task.CompletedTask;
        }
    }
}
