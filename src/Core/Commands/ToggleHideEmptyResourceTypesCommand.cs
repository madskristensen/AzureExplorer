using AzureExplorer.Core.Options;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.Core.Commands
{
    /// <summary>
    /// Toolbar command that toggles the "Hide Empty Resource Types" setting.
    /// When enabled, resource type categories with no children are hidden from the tree.
    /// </summary>
    [Command(PackageIds.ToggleHideEmptyResourceTypes)]
    internal sealed class ToggleHideEmptyResourceTypesCommand : BaseCommand<ToggleHideEmptyResourceTypesCommand>
    {
        protected override void BeforeQueryStatus(EventArgs e)
        {
            // Update the checked state based on the current setting
            Command.Checked = GeneralOptions.Instance.HideEmptyResourceTypes;
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                // Toggle the setting
                GeneralOptions options = GeneralOptions.Instance;
                options.HideEmptyResourceTypes = !options.HideEmptyResourceTypes;
                await options.SaveAsync();

                // Refresh the tree to apply the new setting
                await AzureExplorerControl.ReloadTreeAsync();

                var state = options.HideEmptyResourceTypes ? "enabled" : "disabled";
                await VS.StatusBar.ShowMessageAsync($"Hide empty resource types: {state}");
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                await VS.StatusBar.ShowMessageAsync($"Failed to toggle setting: {ex.Message}");
            }
        }
    }
}
