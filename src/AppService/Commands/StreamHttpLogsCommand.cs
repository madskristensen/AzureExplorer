using AzureExplorer.AppService.Models;
using AzureExplorer.AppService.Services;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.AppService.Commands
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
