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

        public override Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            try
            {
                return Task.FromResult<FrameworkElement>(new AzureExplorerControl());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Azure Explorer: Failed to create tool window content: {ex}");
                return Task.FromResult<FrameworkElement>(
                    new TextBlock { Text = $"Failed to load Azure Explorer:\n{ex.Message}", Margin = new Thickness(10) });
            }
        }

        [Guid("d4b65484-2b5e-4e73-b5a0-9c9f91e1dc21")]
        internal class Pane : ToolWindowPane
        {
            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.AzureResourceGroup;
                ToolBar = new CommandID(PackageGuids.AzureExplorer, PackageIds.ToolWindowToolbar);
                ToolBarLocation = (int)VSTWT_LOCATION.VSTWT_TOP;
            }

            /// <summary>
            /// Enables the search box in the tool window toolbar.
            /// </summary>
            public override bool SearchEnabled => true;

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
