using AzureExplorer.Core.Options;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.Core.Commands
{
    /// <summary>
    /// Toolbar command that toggles the Activity Log panel visibility.
    /// </summary>
    [Command(PackageIds.ToggleActivityLog)]
    internal sealed class ToggleActivityLogCommand : BaseCommand<ToggleActivityLogCommand>
    {
        protected override void BeforeQueryStatus(EventArgs e)
        {
            // Update the checked state based on the current setting
            Command.Checked = GeneralOptions.Instance.ShowActivityLog;
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                // Toggle the setting
                GeneralOptions options = GeneralOptions.Instance;
                options.ShowActivityLog = !options.ShowActivityLog;
                await options.SaveAsync();

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // Notify the control to update visibility
                AzureExplorerControl.SetActivityLogVisible(options.ShowActivityLog);

                var state = options.ShowActivityLog ? "shown" : "hidden";
                await VS.StatusBar.ShowMessageAsync($"Activity Log: {state}");
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                await VS.StatusBar.ShowMessageAsync($"Failed to toggle Activity Log: {ex.Message}");
            }
        }
    }
}
