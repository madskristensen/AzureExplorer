using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace AzureExplorer.AppService.Models
{
    /// <summary>
    /// Represents a file in the App Service file system. Leaf node that can be opened in VS editor.
    /// </summary>
    internal sealed class FileNode : ExplorerNodeBase
    {
        private ImageMoniker? _iconMoniker;

        public FileNode(string name, string subscriptionId, string appName, string relativePath, long size)
            : base(name)
        {
            SubscriptionId = subscriptionId;
            AppName = appName;
            RelativePath = relativePath;
            Size = size;
            Description = FormatSize(size);
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
        /// The path relative to the VFS root (e.g., "site/wwwroot/web.config").
        /// </summary>
        public string RelativePath { get; }

        /// <summary>
        /// The file size in bytes.
        /// </summary>
        public long Size { get; }

        public override ImageMoniker IconMoniker
        {
            get
            {
                if (_iconMoniker == null)
                {
                    _iconMoniker = GetImageMonikerForFile(Label);
                }

                return _iconMoniker.Value;
            }
        }

        public override int ContextMenuId => PackageIds.FileContextMenu;

        public override bool SupportsChildren => false;

        private static ImageMoniker GetImageMonikerForFile(string fileName)
        {
            var imageService = (IVsImageService2)ServiceProvider.GlobalProvider.GetService(typeof(SVsImageService));
            if (imageService != null)
            {
                return imageService.GetImageMonikerForFile(fileName);
            }

            // Fallback to generic document icon if service unavailable
            return KnownMonikers.Document;
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024)
                return $"{bytes / (1024.0 * 1024.0):F1} MB";

            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }
    }
}
