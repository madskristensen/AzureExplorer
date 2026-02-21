using System.IO;
using System.Threading;

using Azure.Storage.Blobs;

using AzureExplorer.Core.Services;
using AzureExplorer.Storage.Models;
using AzureExplorer.ToolWindows;

using Microsoft.Win32;

namespace AzureExplorer.Storage.Commands
{
    [Command(PackageIds.DownloadBlob)]
    internal sealed class DownloadBlobCommand : BaseCommand<DownloadBlobCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode?.ActualNode is not BlobNode blobNode || blobNode.IsDirectory)
                return;

            // Show save file dialog
            var dialog = new SaveFileDialog
            {
                FileName = blobNode.Label,
                Title = "Download Blob",
                Filter = "All Files (*.*)|*.*"
            };

            // Try to set filter based on content type
            if (!string.IsNullOrEmpty(blobNode.ContentType))
            {
                var extension = GetExtensionFromContentType(blobNode.ContentType);
                if (!string.IsNullOrEmpty(extension))
                {
                    dialog.DefaultExt = extension;
                }
            }

            if (dialog.ShowDialog() != true)
                return;

            // Log the activity as in-progress
            var activity = ActivityLogService.Instance.LogActivity(
                "Downloading",
                blobNode.Label,
                "Blob");

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Downloading {blobNode.Label}...");

                // Pass cancellation token for future cancellation support
                await DownloadBlobAsync(blobNode, dialog.FileName, CancellationToken.None);

                activity.Complete();
                await VS.StatusBar.ShowMessageAsync($"Downloaded {blobNode.Label} to {Path.GetFileName(dialog.FileName)}");
            }
            catch (OperationCanceledException)
            {
                activity.Fail("Download was cancelled");
                await VS.StatusBar.ShowMessageAsync($"Download of {blobNode.Label} was cancelled.");
            }
            catch (Exception ex)
            {
                activity.Fail(ex.Message);
                await ex.LogAsync();
                await VS.MessageBox.ShowErrorAsync("Download Failed", ex.Message);
            }
        }

        private static async Task DownloadBlobAsync(BlobNode blobNode, string destinationPath, CancellationToken cancellationToken)
        {
            Azure.Core.TokenCredential credential = AzureResourceService.Instance.GetCredential(blobNode.SubscriptionId);
            var serviceUri = new Uri($"https://{blobNode.AccountName}.blob.core.windows.net");
            var serviceClient = new BlobServiceClient(serviceUri, credential);
            BlobContainerClient containerClient = serviceClient.GetBlobContainerClient(blobNode.ContainerName);
            BlobClient blobClient = containerClient.GetBlobClient(blobNode.BlobPath);

            // Download to file with cancellation support
            await blobClient.DownloadToAsync(destinationPath, cancellationToken);
        }

        private static string GetExtensionFromContentType(string contentType)
        {
            // Common content type to extension mappings
            return contentType?.ToLowerInvariant() switch
            {
                "text/plain" => ".txt",
                "text/html" => ".html",
                "text/css" => ".css",
                "text/javascript" => ".js",
                "application/json" => ".json",
                "application/xml" => ".xml",
                "application/pdf" => ".pdf",
                "image/png" => ".png",
                "image/jpeg" => ".jpg",
                "image/gif" => ".gif",
                "image/svg+xml" => ".svg",
                "application/zip" => ".zip",
                _ => null
            };
        }
    }
}
