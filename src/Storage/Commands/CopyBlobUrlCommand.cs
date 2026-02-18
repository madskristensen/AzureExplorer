using System.Threading.Tasks;
using System.Windows;

using AzureExplorer.Storage.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.Storage.Commands
{
    [Command(PackageIds.CopyBlobUrl)]
    internal sealed class CopyBlobUrlCommand : BaseCommand<CopyBlobUrlCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            string url = null;
            string name = null;

            // Handle both BlobNode and ContainerNode
            if (AzureExplorerControl.SelectedNode is BlobNode blobNode && !blobNode.IsDirectory)
            {
                url = blobNode.BlobUrl;
                name = blobNode.Label;
            }
            else if (AzureExplorerControl.SelectedNode is ContainerNode containerNode)
            {
                url = containerNode.ContainerUrl;
                name = containerNode.Label;
            }
            else
            {
                return;
            }

            if (!string.IsNullOrEmpty(url))
            {
                Clipboard.SetText(url);
                await VS.StatusBar.ShowMessageAsync($"Copied URL for {name}");
            }
        }
    }
}
