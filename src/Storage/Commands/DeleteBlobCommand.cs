using System;
using System.Threading.Tasks;

using Azure.Storage.Blobs;

using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;
using AzureExplorer.Storage.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.Storage.Commands
{
    [Command(PackageIds.DeleteBlob)]
    internal sealed class DeleteBlobCommand : BaseCommand<DeleteBlobCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode is not BlobNode blobNode || blobNode.IsDirectory)
                return;

            // Confirm deletion
            bool confirmed = await VS.MessageBox.ShowConfirmAsync(
                "Delete Blob",
                $"Are you sure you want to delete '{blobNode.Label}'?\n\nThis action cannot be undone.");

            if (!confirmed)
                return;

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Deleting {blobNode.Label}...");

                await DeleteBlobAsync(blobNode);

                await VS.StatusBar.ShowMessageAsync($"Deleted {blobNode.Label}");

                // Refresh parent node to remove deleted blob
                if (blobNode.Parent is ExplorerNodeBase parent && parent.SupportsChildren)
                {
                    await parent.RefreshAsync();
                }
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                await VS.MessageBox.ShowErrorAsync("Delete Failed", ex.Message);
            }
        }

        private static async Task DeleteBlobAsync(BlobNode blobNode)
        {
            Azure.Core.TokenCredential credential = AzureResourceService.Instance.GetCredential(blobNode.SubscriptionId);
            Uri serviceUri = new Uri($"https://{blobNode.AccountName}.blob.core.windows.net");
            BlobServiceClient serviceClient = new BlobServiceClient(serviceUri, credential);
            BlobContainerClient containerClient = serviceClient.GetBlobContainerClient(blobNode.ContainerName);
            BlobClient blobClient = containerClient.GetBlobClient(blobNode.BlobPath);

            await blobClient.DeleteIfExistsAsync();
        }
    }
}
