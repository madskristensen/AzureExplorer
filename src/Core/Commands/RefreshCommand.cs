using AzureExplorer.AppService.Models;
using AzureExplorer.AppService.Services;
using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.Core.Commands
{
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

    [Command(PackageIds.RefreshSubscription)]
    internal sealed class RefreshSubscriptionCommand : BaseCommand<RefreshSubscriptionCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                ExplorerNodeBase node = AzureExplorerControl.SelectedNode;
                if (node != null)
                    await node.RefreshAsync();
            }
            catch (Exception ex)
            {
                await VS.StatusBar.ShowMessageAsync($"Refresh failed: {ex.Message}");
            }
        }
    }

    [Command(PackageIds.RefreshResourceGroup)]
    internal sealed class RefreshResourceGroupCommand : BaseCommand<RefreshResourceGroupCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                ExplorerNodeBase node = AzureExplorerControl.SelectedNode;
                if (node != null)
                    await node.RefreshAsync();
            }
            catch (Exception ex)
            {
                await VS.StatusBar.ShowMessageAsync($"Refresh failed: {ex.Message}");
            }
        }
    }

    [Command(PackageIds.RefreshTenant)]
    internal sealed class RefreshTenantCommand : BaseCommand<RefreshTenantCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                ExplorerNodeBase node = AzureExplorerControl.SelectedNode;
                if (node != null)
                    await node.RefreshAsync();
            }
            catch (Exception ex)
            {
                await VS.StatusBar.ShowMessageAsync($"Refresh failed: {ex.Message}");
            }
        }
    }

    [Command(PackageIds.RefreshAccount)]
    internal sealed class RefreshAccountCommand : BaseCommand<RefreshAccountCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                ExplorerNodeBase node = AzureExplorerControl.SelectedNode;
                if (node != null)
                    await node.RefreshAsync();
            }
            catch (Exception ex)
            {
                await VS.StatusBar.ShowMessageAsync($"Refresh failed: {ex.Message}");
            }
        }
    }

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
