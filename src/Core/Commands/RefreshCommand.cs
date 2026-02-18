using AzureExplorer.AppService.Models;
using AzureExplorer.AppService.Services;
using AzureExplorer.Core.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.Core.Commands
{
    /// <summary>
    /// Toolbar refresh command - refreshes selected node or reloads entire tree.
    /// </summary>
    [Command(PackageIds.Refresh)]
    internal sealed class RefreshCommand : BaseCommand<RefreshCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                ExplorerNodeBase selectedNode = AzureExplorerControl.SelectedNode;
                if (selectedNode != null && selectedNode.SupportsChildren)
                {
                    await selectedNode.RefreshAsync();
                }
                else
                {
                    await AzureExplorerControl.ReloadTreeAsync();
                }
            }
            catch (Exception ex)
            {
                await VS.StatusBar.ShowMessageAsync($"Refresh failed: {ex.Message}");
            }
        }
    }

        /// <summary>
        /// App Service specific refresh - updates the running state from Azure.
        /// </summary>
        [Command(PackageIds.RefreshAppService)]
    internal sealed class RefreshAppServiceCommand : BaseCommand<RefreshAppServiceCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                if (AzureExplorerControl.SelectedNode is not AppServiceNode node) return;

                await VS.StatusBar.ShowMessageAsync($"Refreshing {node.Label}...");
                var state = await AppServiceManager.Instance.GetStateAsync(
                    node.SubscriptionId, node.ResourceGroupName, node.Label);
                node.State = AppServiceNode.ParseState(state);
                await VS.StatusBar.ShowMessageAsync($"{node.Label}: {node.State}");
            }
            catch (Exception ex)
            {
                await VS.StatusBar.ShowMessageAsync($"Refresh failed: {ex.Message}");
            }
        }
    }
}
