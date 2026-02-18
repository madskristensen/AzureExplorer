using AzureExplorer.Core.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.Core.Commands
{
    /// <summary>
    /// Refreshes the selected node in the Azure Explorer tree.
    /// Works with any node that supports children (has <see cref="ExplorerNodeBase.SupportsChildren"/> = true).
    /// </summary>
    [Command(PackageIds.RefreshNode)]
    internal sealed class RefreshNodeCommand : BaseCommand<RefreshNodeCommand>
    {
        protected override void BeforeQueryStatus(EventArgs e)
        {
            ExplorerNodeBase node = AzureExplorerControl.SelectedNode;
            Command.Enabled = node != null && node.SupportsChildren;
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                if (AzureExplorerControl.SelectedNode is ExplorerNodeBase node && node.SupportsChildren)
                {
                    await VS.StatusBar.ShowMessageAsync($"Refreshing {node.Label}...");
                    await node.RefreshAsync();
                    await VS.StatusBar.ShowMessageAsync($"Refreshed {node.Label}");
                }
            }
            catch (Exception ex)
            {
                await VS.StatusBar.ShowMessageAsync($"Refresh failed: {ex.Message}");
            }
        }
    }
}
