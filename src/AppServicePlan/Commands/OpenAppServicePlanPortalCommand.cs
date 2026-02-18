using System.Diagnostics;

using AzureExplorer.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer
{
    [Command(PackageIds.OpenAppServicePlanPortal)]
    internal sealed class OpenAppServicePlanPortalCommand : BaseCommand<OpenAppServicePlanPortalCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode is not AppServicePlanNode node) return;

            var url = $"https://portal.azure.com/#@/resource/subscriptions/{node.SubscriptionId}" +
                      $"/resourceGroups/{node.ResourceGroupName}" +
                      $"/providers/Microsoft.Web/serverfarms/{node.Label}/overview";

            Process.Start(url);

            await Task.CompletedTask;
        }
    }
}
