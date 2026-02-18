using System.Diagnostics;

using AzureExplorer.KeyVault.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.KeyVault.Commands
{
    [Command(PackageIds.OpenKeyVaultPortal)]
    internal sealed class OpenKeyVaultPortalCommand : BaseCommand<OpenKeyVaultPortalCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode is not KeyVaultNode node) return;

            var url = $"https://portal.azure.com/#@/resource/subscriptions/{node.SubscriptionId}" +
                      $"/resourceGroups/{node.ResourceGroupName}" +
                      $"/providers/Microsoft.KeyVault/vaults/{node.Label}/overview";

            Process.Start(url);

            await Task.CompletedTask;
        }
    }
}
