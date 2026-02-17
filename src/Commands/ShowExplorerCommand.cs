using AzureExplorer.ToolWindows;

namespace AzureExplorer
{
    [Command(PackageIds.ShowExplorerWindow)]
    internal sealed class ShowExplorerCommand : BaseCommand<ShowExplorerCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await AzureExplorerWindow.ShowAsync();
        }
    }
}
