using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Storage.Models
{
    /// <summary>
    /// Represents an individual table within an Azure Storage Account.
    /// </summary>
    internal sealed class TableNode(string name, string subscriptionId, string resourceGroupName, string accountName) : ExplorerNodeBase(name)
    {
        public string SubscriptionId { get; } = subscriptionId;
        public string ResourceGroupName { get; } = resourceGroupName;
        public string AccountName { get; } = accountName;

        /// <summary>
        /// Gets the table URL.
        /// </summary>
        public string TableUrl => $"https://{AccountName}.table.core.windows.net/{Label}";

        public override ImageMoniker IconMoniker => KnownMonikers.Table;
        public override int ContextMenuId => PackageIds.TableContextMenu;
        public override bool SupportsChildren => false;
    }
}
