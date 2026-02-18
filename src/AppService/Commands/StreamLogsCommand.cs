using AzureExplorer.Core.Models;
using AzureExplorer.AppService.Services;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.AppService.Commands
{
    [Command(PackageIds.StreamLogs)]
    internal sealed class StreamLogsCommand : BaseCommand<StreamLogsCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode is not IWebSiteNode node) return;

            await LogStreamService.ToggleAsync(node, "application", "application logs");
        }
    }
}
