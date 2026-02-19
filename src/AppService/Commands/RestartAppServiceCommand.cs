using AzureExplorer.AppService.Services;
using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.AppService.Commands
{
    [Command(PackageIds.RestartAppService)]
    internal sealed class RestartAppServiceCommand : BaseCommand<RestartAppServiceCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode?.ActualNode is not IWebSiteNode node) return;

            var confirmed = await VS.MessageBox.ShowConfirmAsync(
                "Restart App Service",
                $"Are you sure you want to restart '{node.Label}'? This will briefly interrupt the service.");

            if (!confirmed) return;

            // Log the activity as in-progress
            var activity = ActivityLogService.Instance.LogActivity(
                "Restarting",
                node.Label,
                "AppService");

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Restarting {node.Label}...");
                await AppServiceManager.Instance.RestartAsync(node.SubscriptionId, node.ResourceGroupName, node.Label);

                node.State = WebSiteState.Running;

                // Mark activity as successful
                activity.Complete();
                await VS.StatusBar.ShowMessageAsync($"{node.Label} restarted.");
            }
            catch (Exception ex)
            {
                // Mark activity as failed
                activity.Fail(ex.Message);
                await VS.MessageBox.ShowErrorAsync("Restart App Service", ex.Message);
            }
        }
    }
}
