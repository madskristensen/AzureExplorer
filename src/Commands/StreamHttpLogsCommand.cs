using AzureExplorer.Models;
using AzureExplorer.Services;
using AzureExplorer.ToolWindows;

namespace AzureExplorer
{
    [Command(PackageIds.StreamHttpLogs)]
    internal sealed class StreamHttpLogsCommand : BaseCommand<StreamHttpLogsCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode is not AppServiceNode node) return;

            await LogStreamService.ToggleAsync(node, "http", "HTTP logs");
        }
    }
}
