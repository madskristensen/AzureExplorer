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
    /// Container node representing the "Containers" section under a Storage Account.
    /// Shows blob containers when expanded.
    /// </summary>
    internal sealed class ContainersNode : ExplorerNodeBase
    {
        public ContainersNode(string subscriptionId, string resourceGroupName, string accountName)
            : base("Blob Containers")
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            AccountName = accountName;

            // Add loading placeholder
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }
        public string AccountName { get; }

        public override ImageMoniker IconMoniker => KnownMonikers.FolderOpened;
        public override int ContextMenuId => 0; // No context menu for containers list
        public override bool SupportsChildren => true;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            try
            {
                BlobServiceClient client = await GetBlobServiceClientAsync();
                var containers = new List<ContainerNode>();

                await foreach (BlobContainerItem container in client.GetBlobContainersAsync(cancellationToken: cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    containers.Add(new ContainerNode(
                        container.Name,
                        SubscriptionId,
                        ResourceGroupName,
                        AccountName,
                        container.Properties.PublicAccess?.ToString()));
                }

                // Sort alphabetically by name
                foreach (ContainerNode node in containers.OrderBy(c => c.Label, StringComparer.OrdinalIgnoreCase))
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

        private async Task<BlobServiceClient> GetBlobServiceClientAsync()
        {
            // Get credential scoped to the subscription's tenant
            Azure.Core.TokenCredential credential = AzureResourceService.Instance.GetCredential(SubscriptionId);

            // Build the blob service URI
            var serviceUri = new Uri($"https://{AccountName}.blob.core.windows.net");

            return new BlobServiceClient(serviceUri, credential);
        }
    }
}
