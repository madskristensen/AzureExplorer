using System.Threading;
using System.Threading.Tasks;

namespace AzureExplorer.Core.Models
{
    /// <summary>
    /// Interface for tree nodes that can accept file and folder drops.
    /// Implemented by nodes like FilesNode and FolderNode to enable drag-and-drop uploads.
    /// </summary>
    internal interface IDropTarget
    {
        /// <summary>
        /// Gets the subscription ID for the target resource.
        /// </summary>
        string SubscriptionId { get; }

        /// <summary>
        /// Gets the App Service or Function App name.
        /// </summary>
        string AppName { get; }

        /// <summary>
        /// Gets the target path relative to the VFS root where files should be uploaded.
        /// For FilesNode this is "site/wwwroot", for FolderNode this is the folder's relative path.
        /// </summary>
        string TargetPath { get; }

        /// <summary>
        /// Uploads files and folders to this node's target path and adds them to the tree.
        /// </summary>
        /// <param name="paths">Array of local file or folder paths to upload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of files uploaded.</returns>
        Task<int> UploadAndAddNodesAsync(string[] paths, CancellationToken cancellationToken = default);
    }
}
