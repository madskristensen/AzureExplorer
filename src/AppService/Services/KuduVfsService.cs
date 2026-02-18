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

        // Shared HttpClient for short-lived operations to prevent socket exhaustion.
        // Auth is set per-request, not in DefaultRequestHeaders, for thread safety.
        private static readonly HttpClient _sharedClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

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
            // Ensure path ends with / for directory listing
            var normalizedPath = path.TrimEnd('/') + "/";
            var url = $"https://{appName}.scm.azurewebsites.net/api/vfs/{normalizedPath}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            await AddAuthenticationAsync(request, subscriptionId, cancellationToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using HttpResponseMessage response = await _sharedClient.SendAsync(request, cancellationToken);
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
        /// <returns>A stream containing the file contents. Dispose to release all resources.</returns>
        public async Task<Stream> DownloadFileAsync(
            string subscriptionId,
            string appName,
            string path,
            CancellationToken cancellationToken = default)
        {
            HttpClient client = await CreateAuthenticatedClientAsync(subscriptionId, cancellationToken);

            var url = $"https://{appName}.scm.azurewebsites.net/api/vfs/{path.TrimStart('/')}";

            HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            Stream contentStream = await response.Content.ReadAsStreamAsync();

            // Return a wrapper that disposes the HttpClient and response when the stream is disposed
            return new HttpResponseStream(contentStream, response, client);
        }

        /// <summary>
        /// A stream wrapper that disposes the underlying HttpClient and HttpResponseMessage
        /// when the stream is disposed, preventing memory leaks.
        /// </summary>
        private sealed class HttpResponseStream : Stream
        {
            private readonly Stream _innerStream;
            private readonly HttpResponseMessage _response;
            private readonly HttpClient _client;
            private bool _disposed;

            public HttpResponseStream(Stream innerStream, HttpResponseMessage response, HttpClient client)
            {
                _innerStream = innerStream;
                _response = response;
                _client = client;
            }

            public override bool CanRead => _innerStream.CanRead;
            public override bool CanSeek => _innerStream.CanSeek;
            public override bool CanWrite => _innerStream.CanWrite;
            public override long Length => _innerStream.Length;
            public override long Position
            {
                get => _innerStream.Position;
                set => _innerStream.Position = value;
            }

            public override void Flush() => _innerStream.Flush();
            public override int Read(byte[] buffer, int offset, int count) => _innerStream.Read(buffer, offset, count);
            public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);
            public override void SetLength(long value) => _innerStream.SetLength(value);
            public override void Write(byte[] buffer, int offset, int count) => _innerStream.Write(buffer, offset, count);

            protected override void Dispose(bool disposing)
            {
                if (!_disposed)
                {
                    if (disposing)
                    {
                        _innerStream?.Dispose();
                        _response?.Dispose();
                        _client?.Dispose();
                    }
                    _disposed = true;
                }
                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// Deletes a file or folder from the App Service file system.
        /// </summary>
        /// <param name="subscriptionId">The subscription ID.</param>
        /// <param name="appName">The App Service name.</param>
        /// <param name="path">The path relative to site root.</param>
        /// <param name="isDirectory">True if deleting a directory, false for a file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task DeleteAsync(
            string subscriptionId,
            string appName,
            string path,
            bool isDirectory,
            CancellationToken cancellationToken = default)
        {
            // For directories, path must end with /
            var normalizedPath = path.TrimStart('/');
            if (isDirectory)
            {
                normalizedPath = normalizedPath.TrimEnd('/') + "/";
            }

            var url = $"https://{appName}.scm.azurewebsites.net/api/vfs/{normalizedPath}";

            using var request = new HttpRequestMessage(HttpMethod.Delete, url);
            await AddAuthenticationAsync(request, subscriptionId, cancellationToken);
            // Kudu VFS API requires If-Match header for delete operations
            request.Headers.Add("If-Match", "*");

            using HttpResponseMessage response = await _sharedClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Adds Azure authentication to an HTTP request.
        /// </summary>
        private static async Task AddAuthenticationAsync(HttpRequestMessage request, string subscriptionId, CancellationToken cancellationToken)
        {
            TokenCredential credential = AzureResourceService.Instance.GetCredential(subscriptionId);
            var context = new TokenRequestContext(new[] { "https://management.azure.com/.default" });
            AccessToken token = await credential.GetTokenAsync(context, cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        }

        /// <summary>
        /// Creates a new HttpClient with authentication for long-running operations (e.g., downloads).
        /// </summary>
        private static async Task<HttpClient> CreateAuthenticatedClientAsync(string subscriptionId, CancellationToken cancellationToken)
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
