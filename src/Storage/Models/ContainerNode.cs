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
    /// Represents a blob container within a Storage Account. Expandable to show blobs.
    /// </summary>
    internal sealed class ContainerNode : ExplorerNodeBase
    {
        public ContainerNode(
            string name,
            string subscriptionId,
            string resourceGroupName,
            string accountName,
            string publicAccess)
            : base(name)
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            AccountName = accountName;
            PublicAccess = publicAccess;

            // Show access level as description if public
            Description = string.IsNullOrEmpty(publicAccess) || publicAccess == "None"
                ? null
                : $"Public: {publicAccess}";

            // Add loading placeholder for blobs
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }
        public string AccountName { get; }
        public string PublicAccess { get; }

        /// <summary>
        /// Gets the container URL.
        /// </summary>
        public string ContainerUrl => $"https://{AccountName}.blob.core.windows.net/{Label}";

        public override ImageMoniker IconMoniker => KnownMonikers.FolderClosed;
        public override int ContextMenuId => PackageIds.BlobContainerContextMenu;
        public override bool SupportsChildren => true;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            try
            {
                BlobContainerClient containerClient = await GetContainerClientAsync();
                var blobs = new List<BlobNode>();

                // List blobs with hierarchy (show virtual directories)
                await foreach (BlobHierarchyItem item in containerClient.GetBlobsByHierarchyAsync(
                    delimiter: "/",
                    cancellationToken: cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (item.IsPrefix)
                    {
                        // Virtual directory - show as folder
                        string folderName = item.Prefix.TrimEnd('/');
                        blobs.Add(new BlobNode(
                            folderName,
                            SubscriptionId,
                            ResourceGroupName,
                            AccountName,
                            Label,
                            item.Prefix,
                            isDirectory: true,
                            size: null,
                            contentType: null));
                    }
                    else if (item.Blob != null)
                    {
                        blobs.Add(new BlobNode(
                            item.Blob.Name,
                            SubscriptionId,
                            ResourceGroupName,
                            AccountName,
                            Label,
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
            return serviceClient.GetBlobContainerClient(Label);
        }
    }
}
