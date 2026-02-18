using System.IO;
using System.Threading;

using AzureExplorer.AppService.Models;

namespace AzureExplorer.AppService.Services
{
    /// <summary>
    /// Downloads files from App Service to a temp location and opens them in the VS editor.
    /// </summary>
    internal static class FileOpenService
    {
        private static readonly string TempBasePath = Path.Combine(Path.GetTempPath(), "AzureExplorer");

        /// <summary>
        /// Downloads a file to a temp location and opens it in the Visual Studio editor.
        /// </summary>
        /// <param name="fileNode">The file node to open.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static async Task OpenFileInEditorAsync(FileNode fileNode, CancellationToken cancellationToken = default)
        {
            if (fileNode == null)
                throw new ArgumentNullException(nameof(fileNode));

            await VS.StatusBar.ShowMessageAsync($"Downloading {fileNode.Label}...");

            try
            {
                // Build temp file path: %TEMP%/AzureExplorer/{appName}/{relativePath}
                var tempFilePath = GetTempFilePath(fileNode.AppName, fileNode.RelativePath);

                // Ensure directory exists
                var directory = Path.GetDirectoryName(tempFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Download the file
                using (Stream sourceStream = await KuduVfsService.Instance.DownloadFileAsync(
                    fileNode.SubscriptionId, fileNode.AppName, fileNode.RelativePath, cancellationToken))
                using (FileStream fileStream = File.Create(tempFilePath))
                {
                    await sourceStream.CopyToAsync(fileStream);
                }

                // Open in VS editor
                await VS.Documents.OpenAsync(tempFilePath);
                await VS.StatusBar.ShowMessageAsync($"Opened {fileNode.Label}");
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                await VS.StatusBar.ShowMessageAsync($"Failed to open {fileNode.Label}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the temp file path for a given app and relative path.
        /// </summary>
        private static string GetTempFilePath(string appName, string relativePath)
        {
            // Normalize the path separators and remove leading slashes
            var normalizedPath = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);

            return Path.Combine(TempBasePath, appName, normalizedPath);
        }
    }
}
