using AzureExplorer.Models;
using AzureExplorer.Services;
using AzureExplorer.ToolWindows;

namespace AzureExplorer
{
    [Command(PackageIds.StopAppService)]
    internal sealed class StopAppServiceCommand : BaseCommand<StopAppServiceCommand>
    {
        protected override void BeforeQueryStatus(EventArgs e)
        {
            // Only visible when the selected App Service is Running
            Command.Visible = AzureExplorerControl.SelectedNode is AppServiceNode node && node.State == AppServiceState.Running;
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode is not AppServiceNode node) return;

            var confirmed = await VS.MessageBox.ShowConfirmAsync(
                "Stop App Service",
                $"Are you sure you want to stop '{node.Label}'? This will make the app unavailable.");

            if (!confirmed) return;

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Stopping {node.Label}...");
                await AppServiceManager.Instance.StopAsync(node.SubscriptionId, node.ResourceGroupName, node.Label);
                node.State = AppServiceState.Stopped;
                await VS.StatusBar.ShowMessageAsync($"{node.Label} stopped.");
            }
            catch (Exception ex)
            {
                await VS.MessageBox.ShowErrorAsync("Stop App Service", ex.Message);
            }
        }
    }
}
