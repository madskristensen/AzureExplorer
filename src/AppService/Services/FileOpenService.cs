using System.IO;
using System.Linq;
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

        // Files older than this will be cleaned up
        private static readonly TimeSpan MaxFileAge = TimeSpan.FromDays(7);

        // Track if cleanup has run this session
        private static bool _cleanupRun;

        /// <summary>
        /// Downloads a file to a temp location and opens it in the Visual Studio editor.
        /// </summary>
        /// <param name="fileNode">The file node to open.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static async Task OpenFileInEditorAsync(FileNode fileNode, CancellationToken cancellationToken = default)
        {
            if (fileNode == null)
                throw new ArgumentNullException(nameof(fileNode));

            // Run cleanup once per session (non-blocking)
            TryCleanupOldFilesAsync().FireAndForget();

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

        /// <summary>
        /// Cleans up old cached files to prevent disk space accumulation.
        /// Runs once per session, non-blocking.
        /// </summary>
        private static async Task TryCleanupOldFilesAsync()
        {
            if (_cleanupRun)
                return;

            _cleanupRun = true;

            await Task.Run(() =>
            {
                try
                {
                    if (!Directory.Exists(TempBasePath))
                        return;

                    DateTime cutoff = DateTime.UtcNow - MaxFileAge;

                    // Clean up old files
                    foreach (var file in Directory.EnumerateFiles(TempBasePath, "*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            DateTime lastAccess = File.GetLastAccessTimeUtc(file);
                            if (lastAccess < cutoff)
                            {
                                File.Delete(file);
                            }
                        }
                        catch
                        {
                            // Ignore files that can't be deleted (may be open)
                        }
                    }

                    // Clean up empty directories
                    foreach (var dir in Directory.EnumerateDirectories(TempBasePath, "*", SearchOption.AllDirectories)
                        .OrderByDescending(d => d.Length)) // Process deepest first
                    {
                        try
                        {
                            if (!Directory.EnumerateFileSystemEntries(dir).Any())
                            {
                                Directory.Delete(dir);
                            }
                        }
                        catch
                        {
                            // Ignore directories that can't be deleted
                        }
                    }
                }
                catch
                {
                    // Cleanup is best-effort, don't fail the operation
                }
            });
        }
    }
}
