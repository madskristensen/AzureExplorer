using AzureExplorer.Models;
using AzureExplorer.Services;
using AzureExplorer.ToolWindows;

namespace AzureExplorer
{
    [Command(PackageIds.StreamLogs)]
    internal sealed class StreamLogsCommand : BaseCommand<StreamLogsCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode is not AppServiceNode node) return;

            await LogStreamService.ToggleAsync(node, "application", "application logs");
        }
    }
}
