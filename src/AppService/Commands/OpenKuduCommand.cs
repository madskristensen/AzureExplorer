using System.Diagnostics;

using AzureExplorer.AppService.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.AppService.Commands
{
    [Command(PackageIds.OpenKudu)]
    internal sealed class OpenKuduCommand : BaseCommand<OpenKuduCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode is not AppServiceNode node) return;

            var url = $"https://{node.Label}.scm.azurewebsites.net/";

            Process.Start(url);
        }
    }
}
