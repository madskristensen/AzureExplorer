using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Storage.Models
{
    /// <summary>
    /// Represents a blob or virtual directory within a container.
    /// </summary>
    internal sealed class BlobNode : ExplorerNodeBase
    {
        public BlobNode(
            string name,
            string subscriptionId,
            string resourceGroupName,
            string accountName,
            string containerName,
            string blobPath,
            bool isDirectory,
            long? size,
            string contentType)
            : base(GetDisplayName(name, isDirectory))
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            AccountName = accountName;
            ContainerName = containerName;
            BlobPath = blobPath;
            IsDirectory = isDirectory;
            Size = size;
            ContentType = contentType;

            // Show size as description for files
            Description = isDirectory ? null : FormatSize(size);

            // Add loading placeholder for directories
            if (isDirectory)
            {
                Children.Add(new LoadingNode());
            }
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }
        public string AccountName { get; }
        public string ContainerName { get; }
        public string BlobPath { get; }
        public bool IsDirectory { get; }
        public long? Size { get; }
        public string ContentType { get; }

        /// <summary>
        /// Gets the full blob URL.
        /// </summary>
        public string BlobUrl => $"https://{AccountName}.blob.core.windows.net/{ContainerName}/{BlobPath}";

        public override ImageMoniker IconMoniker => IsDirectory
            ? KnownMonikers.FolderClosed
            : KnownMonikers.Document;

        public override int ContextMenuId => IsDirectory
            ? PackageIds.BlobFolderContextMenu
            : PackageIds.BlobContextMenu;

        public override bool SupportsChildren => IsDirectory;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!IsDirectory || !BeginLoading())
                return;

            try
            {
                BlobContainerClient containerClient = await GetContainerClientAsync();
                var blobs = new List<BlobNode>();

                // List blobs under this prefix (virtual directory)
                await foreach (BlobHierarchyItem item in containerClient.GetBlobsByHierarchyAsync(
                    delimiter: "/",
                    prefix: BlobPath,
                    cancellationToken: cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (item.IsPrefix)
                    {
                        // Virtual directory
                        var folderName = item.Prefix.TrimEnd('/');
                        // Get just the folder name without parent path
                        var lastSlash = folderName.LastIndexOf('/');
                        var displayName = lastSlash >= 0 ? folderName.Substring(lastSlash + 1) : folderName;

                        blobs.Add(new BlobNode(
                            displayName,
                            SubscriptionId,
                            ResourceGroupName,
                            AccountName,
                            ContainerName,
                            item.Prefix,
                            isDirectory: true,
                            size: null,
                            contentType: null));
                    }
                    else if (item.Blob != null)
                    {
                        // Get just the file name without parent path
                        var blobName = item.Blob.Name;
                        var lastSlash = blobName.LastIndexOf('/');
                        var displayName = lastSlash >= 0 ? blobName.Substring(lastSlash + 1) : blobName;

                        blobs.Add(new BlobNode(
                            displayName,
                            SubscriptionId,
                            ResourceGroupName,
                            AccountName,
                            ContainerName,
                            item.Blob.Name,
                            isDirectory: false,
                            size: item.Blob.Properties.ContentLength,
                            contentType: item.Blob.Properties.ContentType));
                    }
                }

                // Sort: folders first, then files, both alphabetically
                foreach (BlobNode node in blobs
                    .OrderByDescending(b => b.IsDirectory)
                    .ThenBy(b => b.Label, StringComparer.OrdinalIgnoreCase))
                {
                    AddChild(node);
                }
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                Children.Clear();
                Children.Add(new LoadingNode { Label = $"Error: {ex.Message}" });
            }
            finally
            {
                EndLoading();
            }
        }

        private async Task<BlobContainerClient> GetContainerClientAsync()
        {
            Azure.Core.TokenCredential credential = AzureResourceService.Instance.GetCredential(SubscriptionId);
            Uri serviceUri = new Uri($"https://{AccountName}.blob.core.windows.net");
            BlobServiceClient serviceClient = new BlobServiceClient(serviceUri, credential);
            return serviceClient.GetBlobContainerClient(ContainerName);
        }

        private static string GetDisplayName(string name, bool isDirectory)
        {
            if (isDirectory)
            {
                // Remove trailing slash for display
                return name.TrimEnd('/');
            }
            return name;
        }

        private static string FormatSize(long? bytes)
        {
            if (!bytes.HasValue)
                return null;

            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes.Value;
            var suffixIndex = 0;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            return suffixIndex == 0
                ? $"{size:N0} {suffixes[suffixIndex]}"
                : $"{size:N1} {suffixes[suffixIndex]}";
        }
    }
}
