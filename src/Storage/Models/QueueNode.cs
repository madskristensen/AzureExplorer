using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Storage.Models
{
    /// <summary>
    /// Represents an individual queue within an Azure Storage Account.
    /// </summary>
    internal sealed class QueueNode(string name, string subscriptionId, string resourceGroupName, string accountName) : ExplorerNodeBase(name)
    {
        public string SubscriptionId { get; } = subscriptionId;
        public string ResourceGroupName { get; } = resourceGroupName;
        public string AccountName { get; } = accountName;

        /// <summary>
        /// Gets the queue URL.
        /// </summary>
        public string QueueUrl => $"https://{AccountName}.queue.core.windows.net/{Label}";

        public override ImageMoniker IconMoniker => KnownMonikers.TaskList;
        public override int ContextMenuId => PackageIds.QueueContextMenu;
        public override bool SupportsChildren => false;
    }
}
