using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using Azure.Core;

using AzureExplorer.AppService.Models;
using AzureExplorer.Core.Services;

using Newtonsoft.Json;

namespace AzureExplorer.AppService.Services
{
    /// <summary>
    /// Provides access to the Kudu VFS API for browsing and downloading files.
    /// </summary>
    internal sealed class KuduVfsService
    {
        private static readonly Lazy<KuduVfsService> _instance = new(() => new KuduVfsService());

        private KuduVfsService() { }

        public static KuduVfsService Instance => _instance.Value;

        /// <summary>
        /// Lists the contents of a directory in the App Service file system.
        /// </summary>
        /// <param name="subscriptionId">The subscription ID.</param>
        /// <param name="appName">The App Service name.</param>
        /// <param name="path">The path relative to site root (e.g., "site/wwwroot").</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of VFS entries (files and folders).</returns>
        public async Task<IReadOnlyList<VfsEntry>> ListDirectoryAsync(
            string subscriptionId,
            string appName,
            string path,
            CancellationToken cancellationToken = default)
        {
            using HttpClient client = await CreateAuthenticatedClientAsync(subscriptionId, cancellationToken);

            // Ensure path ends with / for directory listing
            var normalizedPath = path.TrimEnd('/') + "/";
            var url = $"https://{appName}.scm.azurewebsites.net/api/vfs/{normalizedPath}";

            using HttpResponseMessage response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            List<VfsEntry> entries = JsonConvert.DeserializeObject<List<VfsEntry>>(json);

            return entries ?? new List<VfsEntry>();
        }

        /// <summary>
        /// Downloads a file from the App Service file system.
        /// </summary>
        /// <param name="subscriptionId">The subscription ID.</param>
        /// <param name="appName">The App Service name.</param>
        /// <param name="path">The path relative to site root (e.g., "site/wwwroot/web.config").</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A stream containing the file contents.</returns>
        public async Task<Stream> DownloadFileAsync(
            string subscriptionId,
            string appName,
            string path,
            CancellationToken cancellationToken = default)
        {
            HttpClient client = await CreateAuthenticatedClientAsync(subscriptionId, cancellationToken);

            var url = $"https://{appName}.scm.azurewebsites.net/api/vfs/{path.TrimStart('/')}";

            // Note: We don't dispose the client here because the caller needs to read the stream.
            // The caller is responsible for disposing the returned stream.
            HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStreamAsync();
        }

        private async Task<HttpClient> CreateAuthenticatedClientAsync(string subscriptionId, CancellationToken cancellationToken)
        {
            TokenCredential credential = AzureResourceService.Instance.GetCredential(subscriptionId);
            var context = new TokenRequestContext(new[] { "https://management.azure.com/.default" });
            AccessToken token = await credential.GetTokenAsync(context, cancellationToken);

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }
    }
}
