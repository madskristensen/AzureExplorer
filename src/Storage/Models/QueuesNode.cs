using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Storage.Models
{
    /// <summary>
    /// Container node representing the "Queues" section under a Storage Account.
    /// Shows storage queues when expanded.
    /// </summary>
    internal sealed class QueuesNode : ExplorerNodeBase
    {
        public QueuesNode(string subscriptionId, string resourceGroupName, string accountName)
            : base("Queues")
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

        public override ImageMoniker IconMoniker => KnownMonikers.TaskList;
        public override int ContextMenuId => 0; // No context menu for queues list
        public override bool SupportsChildren => true;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            try
            {
                QueueServiceClient client = GetQueueServiceClient();
                var queues = new List<QueueNode>();

                await foreach (QueueItem queue in client.GetQueuesAsync(cancellationToken: cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    queues.Add(new QueueNode(
                        queue.Name,
                        SubscriptionId,
                        ResourceGroupName,
                        AccountName));
                }

                // Sort alphabetically by name
                foreach (QueueNode node in queues.OrderBy(q => q.Label, StringComparer.OrdinalIgnoreCase))
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

        private QueueServiceClient GetQueueServiceClient()
        {
            // Get credential scoped to the subscription's tenant
            Azure.Core.TokenCredential credential = AzureResourceService.Instance.GetCredential(SubscriptionId);

            // Build the queue service URI
            var serviceUri = new Uri($"https://{AccountName}.queue.core.windows.net");

            return new QueueServiceClient(serviceUri, credential);
        }
    }
}
