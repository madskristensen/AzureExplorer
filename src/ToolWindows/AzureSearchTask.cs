using System.Collections.Generic;
using System.Threading;
using AzureExplorer.Core.Models;
using AzureExplorer.Core.Search;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace AzureExplorer.ToolWindows;

/// <summary>
/// Search task that performs dual-phase Azure resource search:
/// 1. Instant local search through cached/loaded tree nodes
/// 2. Background API search for additional results
/// </summary>
internal sealed class AzureSearchTask(
    uint dwCookie,
    IVsSearchQuery pSearchQuery,
    IVsSearchCallback pSearchCallback,
    AzureExplorerWindow.Pane toolWindow) : VsSearchTask(dwCookie, pSearchQuery, pSearchCallback), IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly string _searchText = pSearchQuery?.SearchString?.Trim();
    private bool _disposed;

    protected override void OnStartSearch()
    {
        ErrorCode = VSConstants.S_OK;
        var resultCount = 0;

        try
        {
            if (string.IsNullOrEmpty(_searchText))
            {
                SearchResults = 0;
                base.OnStartSearch();
                return;
            }

            // Get cached nodes BEFORE clearing for search (for instant local results)
            IReadOnlyList<ExplorerNodeBase> cachedNodes = toolWindow.GetCachedNodesForSearch();

            // Clear previous results on UI thread using JoinableTaskFactory
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                toolWindow.ClearSearchResults();
            });

            // Run the dual-phase search using JoinableTaskFactory to avoid deadlocks
            // Phase 1: Instant local search through cached nodes
            // Phase 2: Background API search for additional results
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await AzureSearchService.Instance.SearchAllResourcesAsync(
                    _searchText,
                    cachedNodes,
                    onResultFound: result =>
                    {
                        if (_cts.IsCancellationRequested || TaskStatus == VSConstants.VsSearchTaskStatus.Stopped)
                            return;

                        // Add result to UI on main thread
                        ThreadHelper.JoinableTaskFactory.Run(async () =>
                        {
                            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                            toolWindow.AddSearchResult(result);
                        });

                        Interlocked.Increment(ref resultCount);
                    },
                    onProgress: (searched, total) =>
                    {
                        if (_cts.IsCancellationRequested || TaskStatus == VSConstants.VsSearchTaskStatus.Stopped)
                            return;

                        // Report progress (callback is thread-safe per VS SDK docs)
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
                        SearchCallback.ReportProgress(this, searched, total);
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
                    },
                    cancellationToken: _cts.Token);
            });

            SearchResults = (uint)resultCount;
        }
        catch (OperationCanceledException)
        {
            SearchResults = (uint)resultCount;
            ErrorCode = VSConstants.E_ABORT;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Search error: {ex}");
            SearchResults = (uint)resultCount;
            ErrorCode = VSConstants.E_FAIL;
        }

        // Call base implementation to report completion
        base.OnStartSearch();
    }

    protected override void OnStopSearch()
    {
        _cts.Cancel();
        base.OnStopSearch();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cts.Dispose();
            _disposed = true;
        }
    }
}
