using AzureExplorer.Core.Models;
using AzureExplorer.Core.Options;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.Core.Commands
{
    /// <summary>
    /// Toolbar command that toggles the "Show All" setting.
    /// When enabled, hidden subscriptions (shown dimmed) and empty resource types are visible.
    /// </summary>
    [Command(PackageIds.ToggleShowAll)]
    internal sealed class ToggleShowAllCommand : BaseCommand<ToggleShowAllCommand>
    {
        protected override void BeforeQueryStatus(EventArgs e)
        {
            // Update the checked state based on the current setting
            Command.Checked = GeneralOptions.Instance.ShowAll;
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                // Toggle the setting
                GeneralOptions options = GeneralOptions.Instance;
                options.ShowAll = !options.ShowAll;
                await options.SaveAsync();

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // Notify all hideable nodes to update their visibility
                AzureExplorerControl.NotifyAllHideableNodesChanged();

                var state = options.ShowAll ? "enabled" : "disabled";
                await VS.StatusBar.ShowMessageAsync($"Show all: {state}");
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                await VS.StatusBar.ShowMessageAsync($"Failed to toggle setting: {ex.Message}");
            }
        }
    }
}
