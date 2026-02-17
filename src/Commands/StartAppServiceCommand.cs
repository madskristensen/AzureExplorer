using AzureExplorer.Models;
using AzureExplorer.Services;
using AzureExplorer.ToolWindows;

namespace AzureExplorer
{
    [Command(PackageIds.StartAppService)]
    internal sealed class StartAppServiceCommand : BaseCommand<StartAppServiceCommand>
    {
        protected override void BeforeQueryStatus(EventArgs e)
        {
            // Only visible when the selected App Service is Stopped
            Command.Visible = AzureExplorerControl.SelectedNode is AppServiceNode node && node.State != AppServiceState.Running;
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode is not AppServiceNode node) return;

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Starting {node.Label}...");
                await AppServiceManager.Instance.StartAsync(node.SubscriptionId, node.ResourceGroupName, node.Label);
                node.State = AppServiceState.Running;
                await VS.StatusBar.ShowMessageAsync($"{node.Label} started.");
            }
            catch (Exception ex)
            {
                await VS.MessageBox.ShowErrorAsync("Start App Service", ex.Message);
            }
        }
    }
}
