using AzureExplorer.Services;
using AzureExplorer.ToolWindows;

namespace AzureExplorer
{
    [Command(PackageIds.DisconnectLogStream)]
    internal sealed class DisconnectLogStreamCommand : BaseCommand<DisconnectLogStreamCommand>
    {
        protected override void BeforeQueryStatus(EventArgs e)
        {
            // Enable only when there's an active log window with streaming
            var streamKey = LogDocumentWindow.GetActiveStreamKey();
            Command.Enabled = streamKey != null && LogStreamService.IsStreaming(streamKey);
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            var streamKey = LogDocumentWindow.GetActiveStreamKey();
            if (streamKey != null)
            {
                LogStreamService.StopByKey(streamKey);
                await VS.StatusBar.ShowMessageAsync("Log stream disconnected.");
            }
        }
    }
}
