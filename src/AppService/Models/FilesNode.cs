using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
    internal sealed class FilesNode : ExplorerNodeBase, IDropTarget
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

        /// <inheritdoc />
        public string TargetPath => WwwRootPath;

        public override ImageMoniker IconMoniker => KnownMonikers.FolderOpened;

        public override int ContextMenuId => 0; // No context menu for now

        public override bool SupportsChildren => true;

        /// <inheritdoc />
        public async Task<int> UploadAndAddNodesAsync(string[] paths, CancellationToken cancellationToken = default)
        {
            var uploadedCount = 0;

            foreach (var path in paths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (Directory.Exists(path))
                {
                    var dirName = Path.GetFileName(path);
                    var remoteDirPath = $"{TargetPath}/{dirName}";

                    // Create the directory on the remote
                    await KuduVfsService.Instance.CreateDirectoryAsync(SubscriptionId, AppName, remoteDirPath, cancellationToken);

                    // Create folder node and add to tree (or find existing)
                    var folderNode = FindOrCreateFolderNode(dirName, remoteDirPath);

                    // Upload contents recursively
                    uploadedCount += await UploadDirectoryContentsAsync(path, folderNode, cancellationToken);
                }
                else if (File.Exists(path))
                {
                    var fileName = Path.GetFileName(path);
                    var remotePath = $"{TargetPath}/{fileName}";
                    var fileInfo = new FileInfo(path);

                    await KuduVfsService.Instance.UploadFileAsync(SubscriptionId, AppName, remotePath, path, cancellationToken);

                    // Add file node to tree (or update existing)
                    AddOrUpdateFileNode(fileName, remotePath, fileInfo.Length);
                    uploadedCount++;
                }
            }

            return uploadedCount;
        }

        private FolderNode FindOrCreateFolderNode(string name, string remotePath)
        {
            // Check if folder already exists in children
            foreach (var child in Children)
            {
                if (child is FolderNode existing && string.Equals(existing.Label, name, StringComparison.OrdinalIgnoreCase))
                {
                    return existing;
                }
            }

            // Create new folder node and insert in sorted position
            var folderNode = new FolderNode(name, SubscriptionId, AppName, remotePath);
            folderNode.Children.Clear(); // Remove loading placeholder since we'll populate it
            folderNode.IsLoaded = true;
            InsertNodeSorted(folderNode);
            return folderNode;
        }

        private void AddOrUpdateFileNode(string name, string remotePath, long size)
        {
            // Check if file already exists (overwrite case)
            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i] is FileNode existing && string.Equals(existing.Label, name, StringComparison.OrdinalIgnoreCase))
                {
                    // Replace with updated node
                    var newNode = new FileNode(name, SubscriptionId, AppName, remotePath, size) { Parent = this };
                    Children[i] = newNode;
                    return;
                }
            }

            // Insert new file node in sorted position
            var fileNode = new FileNode(name, SubscriptionId, AppName, remotePath, size);
            InsertNodeSorted(fileNode);
        }

        private void InsertNodeSorted(ExplorerNodeBase newNode)
        {
            newNode.Parent = this;
            bool isFolder = newNode is FolderNode;

            for (int i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                bool childIsFolder = child is FolderNode;

                // Folders come before files
                if (isFolder && !childIsFolder)
                {
                    Children.Insert(i, newNode);
                    return;
                }

                // Within same type, sort alphabetically
                if (isFolder == childIsFolder && string.Compare(newNode.Label, child.Label, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    Children.Insert(i, newNode);
                    return;
                }
            }

            Children.Add(newNode);
        }

        private async Task<int> UploadDirectoryContentsAsync(string localDir, FolderNode parentNode, CancellationToken cancellationToken)
        {
            var uploadedCount = 0;

            // Upload all files in this directory
            foreach (var file in Directory.GetFiles(localDir))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fileName = Path.GetFileName(file);
                var remotePath = $"{parentNode.RelativePath}/{fileName}";
                var fileInfo = new FileInfo(file);

                await KuduVfsService.Instance.UploadFileAsync(SubscriptionId, AppName, remotePath, file, cancellationToken);

                // Add file node to folder
                parentNode.AddOrUpdateFileNode(fileName, remotePath, fileInfo.Length);
                uploadedCount++;
            }

            // Recursively upload subdirectories
            foreach (var subDir in Directory.GetDirectories(localDir))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var dirName = Path.GetFileName(subDir);
                var remoteDirPath = $"{parentNode.RelativePath}/{dirName}";

                // Create the directory on the remote
                await KuduVfsService.Instance.CreateDirectoryAsync(SubscriptionId, AppName, remoteDirPath, cancellationToken);

                // Create folder node
                var subFolderNode = parentNode.FindOrCreateFolderNode(dirName, remoteDirPath);

                // Upload contents recursively
                uploadedCount += await UploadDirectoryContentsAsync(subDir, subFolderNode, cancellationToken);
            }

            return uploadedCount;
        }

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
