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
            // Only visible when the selected site is not Running
            Command.Visible = AzureExplorerControl.SelectedNode?.ActualNode is IWebSiteNode node &&
                              node.State != WebSiteState.Running;
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode?.ActualNode is not IWebSiteNode node) return;

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Starting {node.Label}...");
                await AppServiceManager.Instance.StartAsync(node.SubscriptionId, node.ResourceGroupName, node.Label);

                node.State = WebSiteState.Running;

                await VS.StatusBar.ShowMessageAsync($"{node.Label} started.");
            }
            catch (Exception ex)
            {
                await VS.MessageBox.ShowErrorAsync("Start App Service", ex.Message);
            }
        }
    }
}
