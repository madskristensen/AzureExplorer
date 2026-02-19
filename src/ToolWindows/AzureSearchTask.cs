using System.Threading;
using System.Threading.Tasks;
using AzureExplorer.Core.Search;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace AzureExplorer.ToolWindows;

/// <summary>
/// Search task that performs parallel Azure resource search across all accounts.
/// </summary>
internal sealed class AzureSearchTask : VsSearchTask
{
    private readonly AzureExplorerWindow.Pane _toolWindow;
    private readonly CancellationTokenSource _cts;

    public AzureSearchTask(
        uint dwCookie,
        IVsSearchQuery pSearchQuery,
        IVsSearchCallback pSearchCallback,
        AzureExplorerWindow.Pane toolWindow)
        : base(dwCookie, pSearchQuery, pSearchCallback)
    {
        _toolWindow = toolWindow;
        _cts = new CancellationTokenSource();
    }

    protected override void OnStartSearch()
    {
        ErrorCode = VSConstants.S_OK;
        var resultCount = 0;

        try
        {
            var searchText = SearchQuery.SearchString?.Trim();

            if (string.IsNullOrEmpty(searchText))
            {
                SearchResults = 0;
                base.OnStartSearch();
                return;
            }

            // Clear previous results on UI thread
            ThreadHelper.Generic.Invoke(() =>
            {
                _toolWindow.ClearSearchResults();
            });

            // Run the async search synchronously on this background thread
            // The search task is already on a background thread, so we can block here
            Task<uint> searchTask = AzureSearchService.Instance.SearchAllResourcesAsync(
                searchText,
                onResultFound: result =>
                {
                    if (_cts.IsCancellationRequested || TaskStatus == VSConstants.VsSearchTaskStatus.Stopped)
                        return;

                    // Add result to UI on main thread
                    ThreadHelper.Generic.Invoke(() =>
                    {
                        _toolWindow.AddSearchResult(result);
                    });

                    Interlocked.Increment(ref resultCount);
                },
                onProgress: (searched, total) =>
                {
                    if (_cts.IsCancellationRequested || TaskStatus == VSConstants.VsSearchTaskStatus.Stopped)
                        return;

                    // Report progress to VS search host
                    SearchCallback.ReportProgress(this, searched, total);
                },
                cancellationToken: _cts.Token);

            // Wait for completion (we're already on background thread)
            searchTask.GetAwaiter().GetResult();

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
}
