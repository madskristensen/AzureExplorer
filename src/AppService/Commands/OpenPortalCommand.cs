using System.Diagnostics;

using AzureExplorer.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer
{
    [Command(PackageIds.OpenPortal)]
    internal sealed class OpenPortalCommand : BaseCommand<OpenPortalCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode is not AppServiceNode node) return;

            var url = $"https://portal.azure.com/#@/resource/subscriptions/{node.SubscriptionId}" +
                      $"/resourceGroups/{node.ResourceGroupName}" +
                      $"/providers/Microsoft.Web/sites/{node.Label}/overview";

            Process.Start(url);
        }
    }
}
