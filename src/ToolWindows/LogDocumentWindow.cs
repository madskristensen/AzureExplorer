using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

using AzureExplorer.Services;
using Microsoft.VisualStudio.Shell.Interop;

namespace AzureExplorer.ToolWindows
{
    /// <summary>
    /// A document-style window for displaying streaming logs from Azure App Services.
    /// Each app service gets its own document instance.
    /// </summary>
    public class LogDocumentWindow : BaseToolWindow<LogDocumentWindow>
    {
        private static readonly ConcurrentDictionary<string, LogDocumentWindow> _instances = new();
        private static readonly ConcurrentDictionary<int, (string Key, string Caption)> _pendingState = new();
        private static int _nextInstanceId;

        private LogDocumentControl _control;
        private string _logKey;
        private string _caption;
        private int _instanceId;

        public override string GetTitle(int toolWindowId)
        {
            if (_caption != null)
                return _caption;

            if (_pendingState.TryGetValue(toolWindowId, out (string Key, string Caption) state))
                return state.Caption;

            return "Logs";
        }

        public override Type PaneType => typeof(Pane);

        public override System.Threading.Tasks.Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            _instanceId = toolWindowId;

            if (_pendingState.TryRemove(toolWindowId, out (string Key, string Caption) state))
            {
                _logKey = state.Key;
                _caption = state.Caption;
            }

            _control = new LogDocumentControl(_logKey);

            if (_logKey != null)
            {
                _instances[_logKey] = this;
            }

            return System.Threading.Tasks.Task.FromResult<FrameworkElement>(_control);
        }

        private void OnDisconnectRequested()
        {
            if (_logKey != null)
            {
                LogStreamService.StopByKey(_logKey);
            }
        }

        /// <summary>
        /// Gets or creates a log document window for the specified app service and stream type.
        /// </summary>
        public static async System.Threading.Tasks.Task<LogDocumentWindow> GetOrCreateAsync(string appName, string streamType, string streamKey)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (_instances.TryGetValue(streamKey, out LogDocumentWindow existing))
            {
                // Re-show the existing window
                await ShowAsync(0);
                return existing;
            }

            // Short title format: "appName [type]" e.g. "myapp [HTTP]"
            var shortType = streamType;
            if (shortType.EndsWith(" logs", StringComparison.OrdinalIgnoreCase))
                shortType = shortType.Substring(0, shortType.Length - 5);
            if (shortType.Equals("application", StringComparison.OrdinalIgnoreCase))
                shortType = "App";

            // Use unique instance ID for multi-instance support
            var instanceId = System.Threading.Interlocked.Increment(ref _nextInstanceId);

            // Store pending state keyed by instance ID to avoid race conditions
            _pendingState[instanceId] = (streamKey, $"{appName} [{shortType}]");

            await ShowAsync(instanceId);

            // Retrieve the instance that was just created
            _instances.TryGetValue(streamKey, out LogDocumentWindow window);

            // Clean up pending state if CreateAsync didn't consume it (edge case)
            _pendingState.TryRemove(instanceId, out _);

            return window;
        }

        /// <summary>
        /// Removes the window from the instance cache.
        /// </summary>
        public static void Remove(string logKey)
        {
            _instances.TryRemove(logKey, out _);
        }

        /// <summary>
        /// Appends a line of text to the log.
        /// </summary>
        public async System.Threading.Tasks.Task AppendLineAsync(string text)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            _control?.AppendLine(text);
        }

        /// <summary>
        /// Clears all log content.
        /// </summary>
        public async System.Threading.Tasks.Task ClearAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            _control?.Clear();
        }

        /// <summary>
        /// Sets the streaming state to update UI accordingly.
        /// </summary>
        public async System.Threading.Tasks.Task SetStreamingStateAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            _control?.SetStreamingState();
        }

        /// <summary>
        /// Gets the stream key for the currently active log window pane.
        /// </summary>
        public static string GetActiveStreamKey()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (KeyValuePair<string, LogDocumentWindow> kvp in _instances)
            {
                // Check if this instance's pane is active
                if (kvp.Value._control != null)
                {
                    return kvp.Key;
                }
            }

            return null;
        }

        [Guid("a8c7f3e2-1b4d-4e6f-9a2c-8d5e7f1b3c4a")]
        public class Pane : ToolWindowPane
        {
            private LogDocumentControl _control;
            private string _streamKey;

            public Pane()
            {
                BitmapImageMoniker = Microsoft.VisualStudio.Imaging.KnownMonikers.Log;
                ToolBar = new System.ComponentModel.Design.CommandID(PackageGuids.AzureExplorer, PackageIds.LogWindowToolbar);
            }

            public override object Content
            {
                get => base.Content;
                set
                {
                    base.Content = value;
                    _control = value as LogDocumentControl;
                    _streamKey = _control?.StreamKey;
                }
            }

            public override IVsSearchTask CreateSearch(uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchCallback pSearchCallback)
            {
                return new LogSearchTask(dwCookie, pSearchQuery, pSearchCallback, _control);
            }

            public override void ClearSearch()
            {
                _control?.ApplyFilter(null);
            }

            public override void ProvideSearchSettings(IVsUIDataSource pSearchSettings)
            {
                // Use default search settings - instant search with reasonable width
            }

            public override bool OnNavigationKeyDown(uint dwNavigationKey, uint dwModifiers) => false;

            public override bool SearchEnabled => true;

            public override IVsEnumWindowSearchFilters SearchFiltersEnum => null;

            public override IVsEnumWindowSearchOptions SearchOptionsEnum => null;

            protected override void OnClose()
            {
                if (_streamKey != null)
                {
                    // Stop streaming and remove from instances when window is closed
                    LogStreamService.StopByKey(_streamKey);
                    Remove(_streamKey);
                }

                base.OnClose();
            }
        }

        private class LogSearchTask(uint cookie, IVsSearchQuery searchQuery, IVsSearchCallback searchCallback, LogDocumentControl control) : IVsSearchTask
        {
            private volatile uint _status = (uint)__VSSEARCHTASKSTATUS.STS_CREATED;

            public uint Id => cookie;
            public IVsSearchQuery SearchQuery => searchQuery;
            public uint Status => _status;
            public int ErrorCode => 0;

            public void Start()
            {
                _status = (uint)__VSSEARCHTASKSTATUS.STS_STARTED;

                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    control?.ApplyFilter(searchQuery.SearchString);
                    _status = (uint)__VSSEARCHTASKSTATUS.STS_COMPLETED;
                    searchCallback.ReportComplete(this, 0);
                }).FireAndForget();
            }

            public void Stop() => _status = (uint)__VSSEARCHTASKSTATUS.STS_STOPPED;
        }
    }
}
