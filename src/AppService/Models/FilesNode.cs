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
    /// Container node representing the "Files" section under an App Service.
    /// Shows the wwwroot folder contents when expanded.
    /// </summary>
    internal sealed class FilesNode : ExplorerNodeBase
    {
        private const string WwwRootPath = "site/wwwroot";

        public FilesNode(string subscriptionId, string appName)
            : base("Files")
        {
            SubscriptionId = subscriptionId;
            AppName = appName;
            Description = "wwwroot";

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

        public override ImageMoniker IconMoniker => KnownMonikers.FolderOpened;

        public override int ContextMenuId => 0; // No context menu for now

        public override bool SupportsChildren => true;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            try
            {
                IReadOnlyList<VfsEntry> entries = await KuduVfsService.Instance.ListDirectoryAsync(
                    SubscriptionId, AppName, WwwRootPath, cancellationToken);

                // Sort: folders first, then files, both alphabetically
                IEnumerable<VfsEntry> sorted = entries
                    .OrderByDescending(e => e.IsDirectory)
                    .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase);

                foreach (VfsEntry entry in sorted)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var childPath = $"{WwwRootPath}/{entry.Name}";

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
