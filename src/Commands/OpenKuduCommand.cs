using System.Diagnostics;

using AzureExplorer.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer
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
