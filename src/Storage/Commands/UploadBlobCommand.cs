using System.IO;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;
using AzureExplorer.Storage.Models;
using AzureExplorer.ToolWindows;

using Microsoft.Win32;

namespace AzureExplorer.Storage.Commands
{
    [Command(PackageIds.UploadBlob)]
    internal sealed class UploadBlobCommand : BaseCommand<UploadBlobCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            // Get container context - works from ContainerNode or BlobNode (folder)
            string subscriptionId = null;
            string resourceGroupName = null;
            string accountName = null;
            string containerName = null;
            string prefix = null;

            if (AzureExplorerControl.SelectedNode?.ActualNode is ContainerNode containerNode)
            {
                subscriptionId = containerNode.SubscriptionId;
                resourceGroupName = containerNode.ResourceGroupName;
                accountName = containerNode.AccountName;
                containerName = containerNode.Label;
                prefix = "";
            }
            else if (AzureExplorerControl.SelectedNode?.ActualNode is BlobNode blobNode && blobNode.IsDirectory)
            {
                subscriptionId = blobNode.SubscriptionId;
                resourceGroupName = blobNode.ResourceGroupName;
                accountName = blobNode.AccountName;
                containerName = blobNode.ContainerName;
                prefix = blobNode.BlobPath;
            }
            else
            {
                return;
            }

            // Show open file dialog
            var dialog = new OpenFileDialog
            {
                Title = "Upload Blob",
                Filter = "All Files (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                var count = dialog.FileNames.Length;
                var fileLabel = count == 1 ? Path.GetFileName(dialog.FileNames[0]) : $"{count} files";

                // Log the activity as in-progress
                var activity = ActivityLogService.Instance.LogActivity(
                    "Uploading",
                    fileLabel,
                    "Blob");

                await VS.StatusBar.ShowMessageAsync($"Uploading {count} file(s)...");

                await UploadBlobsAsync(subscriptionId, accountName, containerName, prefix, dialog.FileNames);

                activity.Complete();
                await VS.StatusBar.ShowMessageAsync($"Uploaded {count} file(s)");

                // Refresh parent node to show new blobs
                if (AzureExplorerControl.SelectedNode is ExplorerNodeBase parent && parent.SupportsChildren)
                {
                    await parent.RefreshAsync();
                }
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                await VS.MessageBox.ShowErrorAsync("Upload Failed", ex.Message);
            }
        }

        private static async Task UploadBlobsAsync(
            string subscriptionId,
            string accountName,
            string containerName,
            string prefix,
            string[] filePaths)
        {
            Azure.Core.TokenCredential credential = AzureResourceService.Instance.GetCredential(subscriptionId);
            var serviceUri = new Uri($"https://{accountName}.blob.core.windows.net");
            var serviceClient = new BlobServiceClient(serviceUri, credential);
            BlobContainerClient containerClient = serviceClient.GetBlobContainerClient(containerName);

            foreach (var filePath in filePaths)
            {
                var fileName = Path.GetFileName(filePath);
                var blobName = string.IsNullOrEmpty(prefix) ? fileName : $"{prefix}{fileName}";

                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                // Detect content type
                var contentType = GetContentType(fileName);

                var options = new BlobUploadOptions();
                if (!string.IsNullOrEmpty(contentType))
                {
                    options.HttpHeaders = new BlobHttpHeaders { ContentType = contentType };
                }

                await blobClient.UploadAsync(filePath, options);
            }
        }

        private static string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            return extension switch
            {
                ".txt" => "text/plain",
                ".html" or ".htm" => "text/html",
                ".css" => "text/css",
                ".js" => "text/javascript",
                ".json" => "application/json",
                ".xml" => "application/xml",
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".svg" => "image/svg+xml",
                ".zip" => "application/zip",
                ".gz" => "application/gzip",
                ".mp4" => "video/mp4",
                ".mp3" => "audio/mpeg",
                _ => "application/octet-stream"
            };
        }
    }
}
