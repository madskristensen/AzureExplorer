using System.Collections.Generic;
using System.Linq;
using System.Threading;

using AzureExplorer.AppService.Services;
using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.AppService.Models
{
    /// <summary>
    /// Represents a folder in the App Service file system. Expandable node with lazy-loaded children.
    /// </summary>
    internal sealed class FolderNode : ExplorerNodeBase
    {
        public FolderNode(string name, string subscriptionId, string appName, string relativePath)
            : base(name)
        {
            SubscriptionId = subscriptionId;
            AppName = appName;
            RelativePath = relativePath;

            // Add loading placeholder
            Children.Add(new LoadingNode());
        }

        /// <summary>
        /// The subscription ID containing the App Service.
        /// </summary>
        public string SubscriptionId { get; }

        /// <summary>
        /// The App Service name.
        /// </summary>
        public string AppName { get; }

        /// <summary>
        /// The path relative to the VFS root (e.g., "site/wwwroot/css").
        /// </summary>
        public string RelativePath { get; }

        public override ImageMoniker IconMoniker => KnownMonikers.FolderClosed;

        public override int ContextMenuId => 0; // No context menu for now

        public override bool SupportsChildren => true;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            try
            {
                IReadOnlyList<VfsEntry> entries = await KuduVfsService.Instance.ListDirectoryAsync(
                    SubscriptionId, AppName, RelativePath, cancellationToken);

                // Sort: folders first, then files, both alphabetically
                IEnumerable<VfsEntry> sorted = entries
                    .OrderByDescending(e => e.IsDirectory)
                    .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase);

                foreach (VfsEntry entry in sorted)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var childPath = $"{RelativePath.TrimEnd('/')}/{entry.Name}";

                    if (entry.IsDirectory)
                    {
                        AddChild(new FolderNode(entry.Name, SubscriptionId, AppName, childPath));
                    }
                    else
                    {
                        AddChild(new FileNode(entry.Name, SubscriptionId, AppName, childPath, entry.Size));
                    }
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
    }
}
