using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;

using Azure.Core;

using AzureExplorer.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.Services
{
    /// <summary>
    /// Shared streaming logic for Kudu <c>/api/logstream</c> endpoints.
    /// Both application-log and HTTP-log commands delegate here.
    /// </summary>
    internal static class LogStreamService
    {
        private static readonly ConcurrentDictionary<string, CancellationTokenSource> _streams = new();

        /// <summary>
        /// Toggles log streaming for the given node and stream path.
        /// Returns <c>true</c> if a new stream was started, <c>false</c> if an existing one was stopped.
        /// </summary>
        internal static async System.Threading.Tasks.Task<bool> ToggleAsync(AppServiceNode node, string streamPath, string label)
        {
            var key = $"{node.Label}|{streamPath}";

            if (_streams.TryRemove(key, out CancellationTokenSource existingCts))
            {
                existingCts.Cancel();
                existingCts.Dispose();
                await VS.StatusBar.ShowMessageAsync($"Stopped {label} for {node.Label}.");
                return false;
            }

            var cts = new CancellationTokenSource();
            _streams[key] = cts;

            ThreadHelper.JoinableTaskFactory.RunAsync(() => StreamAsync(node, streamPath, label, key, cts.Token)).FireAndForget();
            return true;
        }

        /// <summary>
        /// Stops log streaming for a specific key.
        /// </summary>
        internal static void StopByKey(string key)
        {
            if (_streams.TryRemove(key, out CancellationTokenSource cts))
            {
                cts.Cancel();
                cts.Dispose();
            }
        }

        /// <summary>
        /// Checks if a stream is currently active for the given key.
        /// </summary>
        internal static bool IsStreaming(string key) => _streams.ContainsKey(key);

        internal static void Stop()
        {
            foreach (KeyValuePair<string, CancellationTokenSource> kvp in _streams)
            {
                kvp.Value.Cancel();
                kvp.Value.Dispose();
            }
            _streams.Clear();
        }

        private static async Task StreamAsync(AppServiceNode node, string streamPath, string label, string key, CancellationToken ct)
        {
            LogDocumentWindow logWindow = null;

            try
            {
                logWindow = await LogDocumentWindow.GetOrCreateAsync(node.Label, label, key);
                await logWindow.ClearAsync();
                await logWindow.SetStreamingStateAsync();
                await logWindow.AppendLineAsync($"Enabling logging for {node.Label}...");

                try
                {
                    await AppServiceManager.Instance.EnableApplicationLoggingAsync(
                        node.SubscriptionId, node.ResourceGroupName, node.Label, ct);
                }
                catch (Exception ex)
                {
                    await logWindow.AppendLineAsync($"Warning: Could not enable logging: {ex.Message}");
                    await logWindow.AppendLineAsync("Attempting to connect anyway...");
                }

                // Hit the app so it writes at least one log entry to disk.
                // Kudu disconnects immediately when no log files exist yet.
                // For HTTP logs, use GET instead of HEAD since some configurations
                // don't log HEAD requests to the HTTP log files.
                if (!string.IsNullOrEmpty(node.BrowseUrl))
                {
                    try
                    {
                        using (var warmup = new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
                        {
                            HttpMethod method = streamPath == "http" ? HttpMethod.Get : HttpMethod.Head;
                            var request = new HttpRequestMessage(method, node.BrowseUrl);
                            await warmup.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
                        }
                    }
                    catch
                    {
                        // App may be stopped — ignore
                    }
                }

                await logWindow.AppendLineAsync($"Connecting to {label} for {node.Label}...");

                TokenCredential credential = AzureResourceService.Instance.GetCredential(node.SubscriptionId);
                var context = new TokenRequestContext(["https://management.azure.com/.default"]);
                AccessToken token = await credential.GetTokenAsync(context, ct);

                using (var handler = new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.None })
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token.Token);
                    client.Timeout = Timeout.InfiniteTimeSpan;

                    // Request unbuffered streaming - these headers help reduce server-side buffering
                    client.DefaultRequestHeaders.Add("Accept-Encoding", "identity");  // No compression (causes buffering)
                    client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
                    client.DefaultRequestHeaders.Add("X-Accel-Buffering", "no");       // Disable proxy buffering

                    var url = $"https://{node.Label}.scm.azurewebsites.net/api/logstream/{streamPath}";

                    // Kudu closes the connection immediately when no log files exist
                    // on disk. Retry a few times to give the logging infrastructure
                    // time to flush the first entry from the warmup request above.
                    const int maxAttempts = 5;

                    for (var attempt = 0; attempt < maxAttempts && !ct.IsCancellationRequested; attempt++)
                    {
                        DateTime sessionStart = DateTime.UtcNow;

                        using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct))
                        {
                            response.EnsureSuccessStatusCode();

                            if (attempt == 0)
                            {
                                await logWindow.AppendLineAsync("Connected. Run command again to disconnect.");
                                await VS.StatusBar.ShowMessageAsync($"Streaming {label} for {node.Label}...");
                            }

                            using (Stream stream = await response.Content.ReadAsStreamAsync())
                            // Use a small buffer (128 bytes) to balance real-time streaming
                            // with CPU efficiency. A 1-byte buffer causes excessive system calls.
                            using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 128))
                            {
                                while (!ct.IsCancellationRequested)
                                {
                                    System.Threading.Tasks.Task<string> readTask = reader.ReadLineAsync();
                                    var cancelTask = Task.Delay(Timeout.Infinite, ct);
                                    Task completed = await Task.WhenAny(readTask, cancelTask);

                                    if (completed == cancelTask)
                                        break;

                                    var line = await readTask;
                                    if (line == null)
                                        break;

                                    await logWindow.AppendLineAsync(line);
                                }
                            }
                        }

                        // If the session lasted more than 10 seconds it was a real
                        // session that ended normally — don't retry.
                        if (DateTime.UtcNow - sessionStart > TimeSpan.FromSeconds(10))
                            break;

                        // Quick disconnect — log files probably don't exist yet.
                        if (attempt < maxAttempts - 1 && !ct.IsCancellationRequested)
                        {
                            await logWindow.AppendLineAsync("Waiting for log files...");
                            await Task.Delay(3000, ct);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when user stops streaming
            }
            catch (HttpRequestException ex)
            {
                if (logWindow != null)
                    await logWindow.AppendLineAsync($"Connection failed: {ex.Message}");

                await VS.StatusBar.ShowMessageAsync($"Log stream failed for {node.Label}.");
            }
            catch (Exception ex)
            {
                if (logWindow != null)
                    await logWindow.AppendLineAsync($"Error: {ex.Message}");

                await VS.StatusBar.ShowMessageAsync($"Log stream error: {ex.Message}");
            }
            finally
            {
                // Remove from active streams
                _streams.TryRemove(key, out _);

                if (logWindow != null)
                {
                    await logWindow.AppendLineAsync("Log stream disconnected.");
                    await logWindow.SetStreamingStateAsync();
                }
            }
        }
    }
}
