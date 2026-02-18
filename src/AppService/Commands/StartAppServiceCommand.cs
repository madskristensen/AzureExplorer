using AzureExplorer.AppService.Models;
using AzureExplorer.AppService.Services;
using AzureExplorer.Core.Models;
using AzureExplorer.FunctionApp.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.AppService.Commands
{
    [Command(PackageIds.StartAppService)]
    internal sealed class StartAppServiceCommand : BaseCommand<StartAppServiceCommand>
    {
        protected override void BeforeQueryStatus(EventArgs e)
        {
            // Only visible when the selected site is Stopped
            Command.Visible = AzureExplorerControl.SelectedNode switch
            {
                AppServiceNode node => node.State != AppServiceState.Running,
                FunctionAppNode node => node.State != FunctionAppState.Running,
                _ => false
            };
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode is not IWebSiteNode node) return;

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Starting {node.Label}...");
                await AppServiceManager.Instance.StartAsync(node.SubscriptionId, node.ResourceGroupName, node.Label);

                // Update state on the concrete node type
                if (AzureExplorerControl.SelectedNode is AppServiceNode appNode)
                    appNode.State = AppServiceState.Running;
                else if (AzureExplorerControl.SelectedNode is FunctionAppNode funcNode)
                    funcNode.State = FunctionAppState.Running;

                await VS.StatusBar.ShowMessageAsync($"{node.Label} started.");
            }
            catch (Exception ex)
            {
                await VS.MessageBox.ShowErrorAsync("Start App Service", ex.Message);
            }
        }
    }
}
