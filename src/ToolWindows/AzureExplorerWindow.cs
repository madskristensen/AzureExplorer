using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using AzureExplorer.Core.Models;
using AzureExplorer.Core.Search;
using AzureExplorer.Core.Services;

using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;

namespace AzureExplorer.ToolWindows
{
    public class AzureExplorerWindow : BaseToolWindow<AzureExplorerWindow>
    {
        public override string GetTitle(int toolWindowId) => "Azure Explorer";

        public override Type PaneType => typeof(Pane);

        public override async Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            try
            {
                // Attempt to restore previous session silently before creating the UI
                // This prevents the welcome screen from flashing when cached credentials exist
                if (AzureAuthService.Instance.HasPersistedAccounts())
                {
                    try
                    {
                        await AzureAuthService.Instance.TrySilentSignInAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        await ex.LogAsync();
                    }
                }

                return new AzureExplorerControl();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                return new TextBlock { Text = $"Failed to load Azure Explorer:\n{ex.Message}", Margin = new Thickness(10) };
            }
        }

        [Guid("d4b65484-2b5e-4e73-b5a0-9c9f91e1dc21")]
        internal class Pane : ToolWindowPane
        {
            private static Pane _instance;

            public Pane()
            {
                _instance = this;
                BitmapImageMoniker = KnownMonikers.AzureResourceGroup;
                ToolBar = new CommandID(PackageGuids.AzureExplorer, PackageIds.ToolWindowToolbar);
                ToolBarLocation = (int)VSTWT_LOCATION.VSTWT_TOP;
            }

            /// <summary>
            /// Enables the search box in the tool window toolbar.
            /// </summary>
            public override bool SearchEnabled => true;

            /// <summary>
            /// Sets the search text in the tool window search box and triggers the search.
            /// This allows programmatic searches (e.g., from context menu commands) to
            /// populate the search box so users can see and clear the filter.
            /// </summary>
            internal static void SetSearchText(string searchText)
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (_instance?.SearchHost == null || string.IsNullOrEmpty(searchText))
                    return;

                _instance.SearchHost.SearchAsync(new SearchQuery(searchText));
            }

            /// <summary>
            /// Simple search query implementation for programmatic searches.
            /// </summary>
            private class SearchQuery(string searchString) : IVsSearchQuery
            {
                public string SearchString { get; } = searchString;
                public uint ParseError => 0;

                public uint GetTokens(uint dwMaxTokens, IVsSearchToken[] rgpSearchTokens)
                {
                    return 0;
                }
            }

            /// <summary>
            /// Creates a search task to perform the Azure resource search.
            /// </summary>
            public override IVsSearchTask CreateSearch(uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchCallback pSearchCallback)
            {
                if (pSearchQuery == null || pSearchCallback == null)
                    return null;

                return new AzureSearchTask(dwCookie, pSearchQuery, pSearchCallback, this);
            }

            /// <summary>
            /// Clears search results and restores the tree view.
            /// </summary>
            public override void ClearSearch()
            {
                if (Content is AzureExplorerControl control)
                {
                    control.ShowTreeView();
                }
            }

            /// <summary>
            /// Configures search settings for instant search with progress bar.
            /// </summary>
            public override void ProvideSearchSettings(IVsUIDataSource pSearchSettings)
            {
                // Enable instant search (search as user types)
                Utilities.SetValue(pSearchSettings,
                    SearchSettingsDataSource.SearchStartTypeProperty.Name,
                    (uint)VSSEARCHSTARTTYPE.SST_INSTANT);

                // Show determinate progress bar
                Utilities.SetValue(pSearchSettings,
                    SearchSettingsDataSource.SearchProgressTypeProperty.Name,
                    (uint)VSSEARCHPROGRESSTYPE.SPT_DETERMINATE);

                // Set minimum search string length to 2 characters
                Utilities.SetValue(pSearchSettings,
                    SearchSettingsDataSource.SearchStartMinCharsProperty.Name,
                    (uint)2);

                // Set search watermark text
                Utilities.SetValue(pSearchSettings,
                    SearchSettingsDataSource.SearchWatermarkProperty.Name,
                    "Search Azure resources...");

                // Add delay to avoid too many searches while typing
                Utilities.SetValue(pSearchSettings,
                    SearchSettingsDataSource.SearchStartDelayProperty.Name,
                    (uint)300);
            }

            /// <summary>
            /// Prepares the tree view for search results.
            /// </summary>
            internal void ClearSearchResults()
            {
                if (Content is AzureExplorerControl control)
                {
                    control.BeginSearch();
                }
            }

            /// <summary>
            /// Gets the cached root nodes for instant local search.
            /// </summary>
            internal IReadOnlyList<ExplorerNodeBase> GetCachedNodesForSearch()
            {
                if (Content is AzureExplorerControl control)
                {
                    return control.GetCachedNodesForSearch();
                }
                return [];
            }

            /// <summary>
            /// Adds a search result node to the tree view.
            /// </summary>
            internal void AddSearchResult(SearchResultNode resultNode)
            {
                if (Content is AzureExplorerControl control)
                {
                    control.AddSearchResultNode(resultNode);
                }
            }
        }
    }
}
