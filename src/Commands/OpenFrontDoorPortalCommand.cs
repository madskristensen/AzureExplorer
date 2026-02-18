using System.Diagnostics;

using AzureExplorer.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer
{
    [Command(PackageIds.OpenFrontDoorPortal)]
    internal sealed class OpenFrontDoorPortalCommand : BaseCommand<OpenFrontDoorPortalCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode is not FrontDoorNode node) return;

            var url = $"https://portal.azure.com/#@/resource/subscriptions/{node.SubscriptionId}" +
                      $"/resourceGroups/{node.ResourceGroupName}" +
                      $"/providers/Microsoft.Cdn/profiles/{node.Label}/overview";

            Process.Start(url);

            await Task.CompletedTask;
        }
    }
}
