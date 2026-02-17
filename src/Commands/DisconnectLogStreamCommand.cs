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
            Command.Enabled = LogDocumentWindow.GetActiveStreamKey() != null &&
                              LogStreamService.IsStreaming(LogDocumentWindow.GetActiveStreamKey());
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            string streamKey = LogDocumentWindow.GetActiveStreamKey();
            if (streamKey != null)
            {
                LogStreamService.StopByKey(streamKey);
                await VS.StatusBar.ShowMessageAsync("Log stream disconnected.");
            }
        }
    }
}
