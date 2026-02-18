using AzureExplorer.AppService.Models;
using AzureExplorer.AppService.Services;
using AzureExplorer.Core.Models;
using AzureExplorer.FunctionApp.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.AppService.Commands
{
    [Command(PackageIds.StopAppService)]
    internal sealed class StopAppServiceCommand : BaseCommand<StopAppServiceCommand>
    {
        protected override void BeforeQueryStatus(EventArgs e)
        {
            // Only visible when the selected site is Running
            Command.Visible = AzureExplorerControl.SelectedNode switch
            {
                AppServiceNode node => node.State == AppServiceState.Running,
                FunctionAppNode node => node.State == FunctionAppState.Running,
                _ => false
            };
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode is not IWebSiteNode node) return;

            var confirmed = await VS.MessageBox.ShowConfirmAsync(
                "Stop App Service",
                $"Are you sure you want to stop '{node.Label}'? This will make the app unavailable.");

            if (!confirmed) return;

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Stopping {node.Label}...");
                await AppServiceManager.Instance.StopAsync(node.SubscriptionId, node.ResourceGroupName, node.Label);

                // Update state on the concrete node type
                if (AzureExplorerControl.SelectedNode is AppServiceNode appNode)
                    appNode.State = AppServiceState.Stopped;
                else if (AzureExplorerControl.SelectedNode is FunctionAppNode funcNode)
                    funcNode.State = FunctionAppState.Stopped;

                await VS.StatusBar.ShowMessageAsync($"{node.Label} stopped.");
            }
            catch (Exception ex)
            {
                await VS.MessageBox.ShowErrorAsync("Stop App Service", ex.Message);
            }
        }
    }
}
