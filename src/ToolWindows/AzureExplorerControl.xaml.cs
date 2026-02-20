using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

using AzureExplorer.AppService.Models;
using AzureExplorer.AppService.Services;
using AzureExplorer.Core.Models;
using AzureExplorer.Core.Options;
using AzureExplorer.Core.Search;
using AzureExplorer.Core.Services;
using AzureExplorer.FunctionApp.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell.Interop;

namespace AzureExplorer.ToolWindows
{
    public partial class AzureExplorerControl : UserControl
    {
        private static AzureExplorerControl _instance;
        private readonly List<ExplorerNodeBase> _savedRootNodes = [];
        private bool _isSearchActive;
        private static ExplorerNodeBase _rightClickedNode;

        public AzureExplorerControl()
        {
            InitializeComponent();
            _instance = this;

            RootNodes = [];

            SetupTreeView();
            SetupActivityLog();

            AzureAuthService.Instance.AuthStateChanged += OnAuthStateChanged;
            Unloaded += OnUnloaded;

            // Silent sign-in is handled in AzureExplorerWindow.CreateAsync before this control is created,
            // so we can directly show the appropriate UI based on current auth state
            RefreshRootNodes();
        }

        internal ObservableCollection<ExplorerNodeBase> RootNodes { get; }

        /// <summary>
        /// Gets the singleton instance of the Azure Explorer control.
        /// </summary>
        internal static AzureExplorerControl Instance => _instance;

        /// <summary>
        /// Gets the node that was right-clicked for context menu operations.
        /// This is more reliable than SelectedNode because it's set synchronously
        /// during the right-click event, before BeforeQueryStatus runs.
        /// </summary>
        internal static ExplorerNodeBase RightClickedNode => _rightClickedNode;

        internal static ExplorerNodeBase SelectedNode => _instance?.ExplorerTree.SelectedItem as ExplorerNodeBase;

        internal static async System.Threading.Tasks.Task ReloadTreeAsync()
        {
            if (_instance != null)
                await _instance.ReloadAsync();
        }

        /// <summary>
        /// Notifies all TenantNode and SubscriptionNode instances in the tree to update their visibility.
        /// Called when the ShowAll setting is toggled.
        /// </summary>
        internal static void NotifyAllHideableNodesChanged()
        {
            if (_instance == null)
                return;

            foreach (ExplorerNodeBase accountNode in _instance.RootNodes)
            {
                foreach (ExplorerNodeBase tenantNode in accountNode.Children)
                {
                    if (tenantNode is TenantNode tenant)
                    {
                        tenant.NotifyVisibilityChanged();

                        foreach (ExplorerNodeBase subscriptionNode in tenant.Children)
                        {
                            if (subscriptionNode is SubscriptionNode subscription)
                            {
                                subscription.NotifyVisibilityChanged();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Configures the TreeView entirely in code to avoid XAML assembly resolution issues.
        /// </summary>
        private void SetupTreeView()
        {
            // Data template: StackPanel with Icon + Health Overlay + Label + Description text
            var stackFactory = new FrameworkElementFactory(typeof(StackPanel));
            stackFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            stackFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 2, 0, 2));
            stackFactory.SetBinding(UIElement.OpacityProperty, new Binding("Opacity"));

            // Main resource icon
            var imageFactory = new FrameworkElementFactory(typeof(CrispImage));
            imageFactory.SetBinding(CrispImage.MonikerProperty, new Binding("IconMoniker"));
            imageFactory.SetValue(FrameworkElement.WidthProperty, 16.0);
            imageFactory.SetValue(FrameworkElement.HeightProperty, 16.0);
            imageFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 4, 0));
            imageFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            stackFactory.AppendChild(imageFactory);

            // Health overlay icon (small status indicator after main icon)
            var healthOverlayFactory = new FrameworkElementFactory(typeof(CrispImage));
            healthOverlayFactory.SetBinding(CrispImage.MonikerProperty, new Binding("HealthOverlayIcon"));
            healthOverlayFactory.SetValue(FrameworkElement.WidthProperty, 10.0);
            healthOverlayFactory.SetValue(FrameworkElement.HeightProperty, 10.0);
            healthOverlayFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(-8, 6, 2, 0)); // Overlap bottom-right of main icon
            healthOverlayFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            stackFactory.AppendChild(healthOverlayFactory);

            var labelFactory = new FrameworkElementFactory(typeof(TextBlock));
            labelFactory.SetBinding(TextBlock.TextProperty, new Binding("Label"));
            labelFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            stackFactory.AppendChild(labelFactory);

            var descFactory = new FrameworkElementFactory(typeof(TextBlock));
            descFactory.SetBinding(TextBlock.TextProperty, new Binding("Description"));
            descFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(6, 0, 0, 0));
            descFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            descFactory.SetValue(UIElement.OpacityProperty, 0.6);
            descFactory.SetValue(TextBlock.FontStyleProperty, FontStyles.Italic);
            stackFactory.AppendChild(descFactory);

            var template = new HierarchicalDataTemplate(typeof(ExplorerNodeBase))
            {
                ItemsSource = new Binding("Children"),
                VisualTree = stackFactory
            };

            ExplorerTree.Resources.Add(new DataTemplateKey(typeof(ExplorerNodeBase)), template);

            // ItemContainerStyle: Add Visibility binding to the existing XAML-defined style
            // This allows nodes to stay in the tree but be hidden/shown without reload
            var xamlStyle = ExplorerTree.ItemContainerStyle;
            if (xamlStyle != null)
            {
                var itemContainerStyle = new Style(typeof(TreeViewItem), xamlStyle);
                itemContainerStyle.Setters.Add(new Setter(
                    TreeViewItem.VisibilityProperty,
                    new Binding("IsVisible") { Converter = new BooleanToVisibilityConverter() }));
                ExplorerTree.ItemContainerStyle = itemContainerStyle;
            }

            // Bind the collection
            ExplorerTree.ItemsSource = RootNodes;

            // Wire events
            ExplorerTree.AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(TreeViewItem_Expanded));
            ExplorerTree.PreviewMouseRightButtonDown += ExplorerTree_PreviewMouseRightButtonDown;
            ExplorerTree.PreviewMouseRightButtonUp += ExplorerTree_PreviewMouseRightButtonUp;
            ExplorerTree.MouseDoubleClick += ExplorerTree_MouseDoubleClick;

            // Enable drag-and-drop for file uploads
            ExplorerTree.AllowDrop = true;
            ExplorerTree.DragOver += ExplorerTree_DragOver;
            ExplorerTree.Drop += ExplorerTree_Drop;
        }

        public void RefreshRootNodes()
        {
            RootNodes.Clear();

            IReadOnlyList<AccountInfo> accounts = AzureAuthService.Instance.Accounts;
            if (accounts.Count == 0)
            {
                // Show empty state panel, hide tree view
                ExplorerTree.Visibility = Visibility.Collapsed;
                EmptyStatePanel.Visibility = Visibility.Visible;
                return;
            }

            // Show tree view, hide empty state panel
            ExplorerTree.Visibility = Visibility.Visible;
            EmptyStatePanel.Visibility = Visibility.Collapsed;

            // Create an AccountNode for each signed-in account
            foreach (AccountInfo account in accounts.OrderBy(a => a.Username, StringComparer.OrdinalIgnoreCase))
            {
                var accountNode = new AccountNode(account.AccountId, account.Username);
                RootNodes.Add(accountNode);

                // Load children in background (don't expand automatically)
                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    try
                    {
                        await accountNode.LoadChildrenAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to load account children: {ex.Message}");
                    }
                }).FireAndForget();
            }
        }

        public async System.Threading.Tasks.Task ReloadAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            RefreshRootNodes();
        }

        private void OnAuthStateChanged(object sender, EventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    RefreshRootNodes();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Auth state change handler failed: {ex.Message}");
                }
            }).FireAndForget();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Unsubscribe from events to prevent memory leaks
            AzureAuthService.Instance.AuthStateChanged -= OnAuthStateChanged;
            Unloaded -= OnUnloaded;

            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await AzureAuthService.Instance.AddAccountAsync();
                    await VS.StatusBar.ShowMessageAsync("Signed in to Azure successfully.");
                }
                catch (OperationCanceledException)
                {
                    await VS.StatusBar.ShowMessageAsync("Azure sign-in was cancelled.");
                }
                catch (Exception ex)
                {
                    await VS.MessageBox.ShowErrorAsync("Azure Sign In", ex.Message);
                }
            }).FireAndForget();
        }

        private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is TreeViewItem treeViewItem &&
                treeViewItem.DataContext is ExplorerNodeBase node &&
                !node.IsLoaded &&
                node.SupportsChildren)
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    try
                    {
                        await node.LoadChildrenAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to load children for {node.Label}: {ex.Message}");
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        node.Children.Clear();
                        node.Children.Add(new LoadingNode { Label = $"Error: {ex.Message}" });
                    }
                }).FireAndForget();

                e.Handled = true;
            }
        }

        private void ExplorerTree_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;
            while (source != null && source is not TreeViewItem)
            {
                source = VisualTreeHelper.GetParent(source);
            }

            if (source is TreeViewItem item)
            {
                item.IsSelected = true;
                item.Focus();

                // Store the right-clicked node for context menu commands.
                // This is set synchronously before BeforeQueryStatus runs,
                // ensuring commands see the correct node.
                _rightClickedNode = item.DataContext as ExplorerNodeBase;

                e.Handled = true;
            }
            else
            {
                _rightClickedNode = null;
            }
        }

        private void ExplorerTree_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ExplorerTree.SelectedItem is not ExplorerNodeBase node || node.ContextMenuId == 0)
                return;

            ShowVsContextMenu(node.ContextMenuId, e);
            e.Handled = true;
        }

        private void ExplorerTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var node = ExplorerTree.SelectedItem;

            if (node is SignInNode)
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    try
                    {
                        await AzureAuthService.Instance.AddAccountAsync();
                    }
                    catch (Exception ex)
                    {
                        await VS.MessageBox.ShowErrorAsync("Azure Sign In", ex.Message);
                    }
                }).FireAndForget();

                e.Handled = true;
            }
            else if (node is AppServiceNode appNode && !string.IsNullOrEmpty(appNode.BrowseUrl))
            {
                Process.Start(appNode.BrowseUrl);
                e.Handled = true;
            }
            else if (node is FileNode fileNode)
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    try
                    {
                        await FileOpenService.OpenFileInEditorAsync(fileNode);
                    }
                    catch (Exception ex)
                    {
                        await VS.MessageBox.ShowErrorAsync("Open File", $"Failed to open file: {ex.Message}");
                    }
                }).FireAndForget();

                e.Handled = true;
            }
        }

        private void ShowVsContextMenu(int menuId, MouseButtonEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var shell = (IVsUIShell)ServiceProvider.GlobalProvider.GetService(typeof(SVsUIShell));
            if (shell == null)
                return;

            // Force VS to refresh command states before showing the menu.
            // Without this, BeforeQueryStatus is only called once and cached.
            shell.UpdateCommandUI(1); // 1 = fImmediateUpdate

            UIElement source = e.OriginalSource as UIElement ?? this;
            Point screenPoint = source.PointToScreen(e.GetPosition(source));

            Guid guid = PackageGuids.AzureExplorer;
            var points = new POINTS[]
            {
                new() {
                    x = (short)screenPoint.X,
                    y = (short)screenPoint.Y
                }
            };

            shell.ShowContextMenu(0, ref guid, menuId, points, null);
        }

        /// <summary>
        /// Shows the tree view and restores original nodes after search is cleared.
        /// </summary>
        internal void ShowTreeView()
        {
            if (_isSearchActive && _savedRootNodes.Count > 0)
            {
                // Restore saved nodes
                RootNodes.Clear();
                foreach (ExplorerNodeBase node in _savedRootNodes)
                {
                    RootNodes.Add(node);
                }
                _savedRootNodes.Clear();
            }

            _isSearchActive = false;

            if (RootNodes.Count > 0)
            {
                ExplorerTree.Visibility = Visibility.Visible;
                EmptyStatePanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                ExplorerTree.Visibility = Visibility.Collapsed;
                EmptyStatePanel.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Prepares the tree view for search results by saving current nodes.
        /// </summary>
        internal void BeginSearch()
        {
            if (!_isSearchActive)
            {
                // Save current nodes before clearing for search
                _savedRootNodes.Clear();
                foreach (ExplorerNodeBase node in RootNodes)
                {
                    _savedRootNodes.Add(node);
                }
                RootNodes.Clear();
                _isSearchActive = true;
            }
            else
            {
                // Already in search mode, just clear results
                RootNodes.Clear();
            }

            ExplorerTree.Visibility = Visibility.Visible;
            EmptyStatePanel.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Gets the cached root nodes for local search (instant results).
        /// Returns the saved nodes if search is active, otherwise the current root nodes.
        /// </summary>
        internal IReadOnlyList<ExplorerNodeBase> GetCachedNodesForSearch()
        {
            return _isSearchActive ? _savedRootNodes : (IReadOnlyList<ExplorerNodeBase>)RootNodes;
        }

        /// <summary>
        /// Adds a search result node to the tree view, maintaining Account > Subscription hierarchy.
        /// </summary>
        internal void AddSearchResultNode(SearchResultNode resultNode)
        {
            if (!_isSearchActive)
                return;

            // Find or create the account node
            SearchAccountNode accountNode = null;
            foreach (ExplorerNodeBase node in RootNodes)
            {
                if (node is SearchAccountNode acct && acct.Label == resultNode.AccountName)
                {
                    accountNode = acct;
                    break;
                }
            }

            if (accountNode == null)
            {
                accountNode = new SearchAccountNode(resultNode.AccountName, resultNode.AccountName);
                RootNodes.Add(accountNode);
            }

            // Find or create the subscription node under the account
            SearchSubscriptionNode subscriptionNode = accountNode.GetOrCreateSubscription(
                            resultNode.SubscriptionId,
                            resultNode.SubscriptionName);

            // Add the result under the subscription
            subscriptionNode.AddResult(resultNode);
        }

        #region Activity Log

        private ItemsControl _activityLogList;
        private TextBlock _activityLogEmptyMessage;
        private Border _activityLogPanel;
        private GridSplitter _activityLogSplitter;
        private RowDefinition _activityLogRow;

        private void SetupActivityLog()
        {
            _activityLogList = (ItemsControl)FindName("ActivityLogList");
            _activityLogEmptyMessage = (TextBlock)FindName("ActivityLogEmptyMessage");
            _activityLogPanel = (Border)FindName("ActivityLogPanel");
            _activityLogSplitter = (GridSplitter)FindName("ActivityLogSplitter");
            _activityLogRow = (RowDefinition)FindName("ActivityLogRow");

            if (_activityLogList != null)
            {
                _activityLogList.ItemsSource = ActivityLogService.Instance.Activities;
            }

            // Restore persisted height
            if (_activityLogRow != null)
            {
                double savedHeight = GeneralOptions.Instance.ActivityLogPanelHeight;
                if (savedHeight > 0)
                {
                    _activityLogRow.Height = new GridLength(savedHeight);
                }
            }

            // Restore visibility from settings
            SetActivityLogVisibleInternal(GeneralOptions.Instance.ShowActivityLog);

            // Update empty message visibility when activities change
            ActivityLogService.Instance.Activities.CollectionChanged += (s, e) =>
            {
                UpdateEmptyMessageVisibility();
            };

            UpdateEmptyMessageVisibility();
        }

        /// <summary>
        /// Called by ToggleActivityLogCommand to show/hide the Activity Log panel.
        /// </summary>
        internal static void SetActivityLogVisible(bool visible)
        {
            _instance?.SetActivityLogVisibleInternal(visible);
        }

        private void SetActivityLogVisibleInternal(bool visible)
        {
            if (_activityLogPanel != null)
            {
                _activityLogPanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            }
            if (_activityLogSplitter != null)
            {
                _activityLogSplitter.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            }
            if (_activityLogRow != null)
            {
                if (visible)
                {
                    double savedHeight = GeneralOptions.Instance.ActivityLogPanelHeight;
                    _activityLogRow.Height = new GridLength(savedHeight > 0 ? savedHeight : 120);
                    _activityLogRow.MinHeight = 60;
                }
                else
                {
                    _activityLogRow.Height = new GridLength(0);
                    _activityLogRow.MinHeight = 0;
                }
            }
        }

        private void ActivityLogSplitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            // Persist the new height
            if (_activityLogRow != null && _activityLogRow.ActualHeight > 0)
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    try
                    {
                        GeneralOptions.Instance.ActivityLogPanelHeight = _activityLogRow.ActualHeight;
                        await GeneralOptions.Instance.SaveAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to save Activity Log height: {ex.Message}");
                    }
                }).FireAndForget();
            }
        }

        private void UpdateEmptyMessageVisibility()
        {
            if (_activityLogEmptyMessage == null)
                return;

            _activityLogEmptyMessage.Visibility = ActivityLogService.Instance.Activities.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void ClearActivityLogButton_Click(object sender, RoutedEventArgs e)
        {
            ActivityLogService.Instance.Clear();
        }

        private void ActivityEntry_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement element || element.DataContext is not ActivityEntry entry)
                return;

            if (string.IsNullOrEmpty(entry.ResourceName))
                return;

            // Try to find and select the resource in the tree by name
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var node = FindNodeByName(RootNodes, entry.ResourceName, entry.ResourceType);
                    if (node != null)
                    {
                        SelectAndExpandToNode(node);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to navigate to resource: {ex.Message}");
                }
            }).FireAndForget();
        }

        private ExplorerNodeBase FindNodeByName(IEnumerable<ExplorerNodeBase> nodes, string name, string resourceType)
        {
            foreach (var node in nodes)
            {
                // Check if this node matches by name
                if (string.Equals(node.Label, name, StringComparison.OrdinalIgnoreCase))
                {
                    // Verify it's the right type of resource
                    bool isMatch = resourceType switch
                    {
                        "AppService" => node is AppServiceNode,
                        "FunctionApp" => node is FunctionAppNode,
                        _ => true
                    };

                    if (isMatch)
                        return node;
                }

                if (node.Children != null && node.Children.Count > 0)
                {
                    var found = FindNodeByName(node.Children, name, resourceType);
                    if (found != null)
                        return found;
                }
            }
            return null;
        }

        private void SelectAndExpandToNode(ExplorerNodeBase targetNode)
        {
            // Build path from root to target
            var path = new List<ExplorerNodeBase>();
            BuildPathToNode(RootNodes, targetNode, path);

            if (path.Count == 0)
                return;

            // Expand each node in the path
            foreach (var node in path)
            {
                node.IsExpanded = true;
            }

            // Find and select the TreeViewItem
            var container = FindTreeViewItemForNode(targetNode);
            if (container != null)
            {
                container.IsSelected = true;
                container.BringIntoView();
                container.Focus();
            }
        }

        private bool BuildPathToNode(IEnumerable<ExplorerNodeBase> nodes, ExplorerNodeBase target, List<ExplorerNodeBase> path)
        {
            foreach (var node in nodes)
            {
                if (node == target)
                    return true;

                if (node.Children != null && node.Children.Count > 0)
                {
                    path.Add(node);
                    if (BuildPathToNode(node.Children, target, path))
                        return true;
                    path.RemoveAt(path.Count - 1);
                }
            }
            return false;
        }

        private TreeViewItem FindTreeViewItemForNode(ExplorerNodeBase node)
        {
            return FindTreeViewItem(ExplorerTree, node);
        }

        private TreeViewItem FindTreeViewItem(ItemsControl container, ExplorerNodeBase node)
        {
            if (container == null)
                return null;

            foreach (var item in container.Items)
            {
                var treeViewItem = container.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                if (treeViewItem == null)
                    continue;

                if (item == node)
                    return treeViewItem;

                var result = FindTreeViewItem(treeViewItem, node);
                if (result != null)
                    return result;
            }
            return null;
        }

        #endregion

        #region Drag and Drop

        private void ExplorerTree_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;

            // Check if we have file data
            if (!HasFilePaths(e.Data))
                return;

            // Find the node under the cursor
            IDropTarget dropTarget = GetDropTargetUnderCursor(e);
            if (dropTarget != null)
            {
                e.Effects = DragDropEffects.Copy;
            }

            e.Handled = true;
        }

        private void ExplorerTree_Drop(object sender, DragEventArgs e)
        {
            IDropTarget dropTarget = GetDropTargetUnderCursor(e);
            if (dropTarget == null)
                return;

            string[] filePaths = GetFilePaths(e.Data);
            if (filePaths == null || filePaths.Length == 0)
                return;

            e.Handled = true;

            // Perform upload asynchronously
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await VS.StatusBar.ShowMessageAsync($"Uploading {filePaths.Length} item(s)...");

                    int uploadedCount = await dropTarget.UploadAndAddNodesAsync(filePaths);

                    await VS.StatusBar.ShowMessageAsync($"Uploaded {uploadedCount} file(s)");
                }
                catch (OperationCanceledException)
                {
                    await VS.StatusBar.ShowMessageAsync("Upload cancelled");
                }
                catch (Exception ex)
                {
                    await ex.LogAsync();
                    await VS.MessageBox.ShowErrorAsync("Upload Failed", ex.Message);
                }
            }).FireAndForget();
        }

        private IDropTarget GetDropTargetUnderCursor(DragEventArgs e)
        {
            if (e.OriginalSource is not DependencyObject source)
                return null;

            // Walk up the visual tree to find the TreeViewItem
            while (source != null && source is not TreeViewItem)
            {
                source = VisualTreeHelper.GetParent(source);
            }

            if (source is TreeViewItem item && item.DataContext is ExplorerNodeBase node)
            {
                // Check the actual node (unwrap SearchResultNode if needed)
                ExplorerNodeBase actualNode = node.ActualNode;
                if (actualNode is IDropTarget dropTarget)
                {
                    return dropTarget;
                }
            }

            return null;
        }

        private static bool HasFilePaths(IDataObject data)
        {
            // Check for Windows Explorer file drop
            if (data.GetDataPresent(DataFormats.FileDrop))
                return true;

            // Check for Solution Explorer items (CF_VSSTGPROJECTITEMS or CF_VSREFPROJECTITEMS)
            if (data.GetDataPresent("CF_VSSTGPROJECTITEMS") || data.GetDataPresent("CF_VSREFPROJECTITEMS"))
                return true;

            return false;
        }

        private static string[] GetFilePaths(IDataObject data)
        {
            // Try Windows Explorer file drop first
            if (data.GetDataPresent(DataFormats.FileDrop))
            {
                return data.GetData(DataFormats.FileDrop) as string[];
            }

            // Try Solution Explorer items
            // Solution Explorer uses a custom format that contains file paths
            try
            {
                // For Solution Explorer, try to get the file paths from the clipboard format
                // The format contains DROPFILES structure followed by null-terminated paths
                if (data.GetDataPresent("CF_VSSTGPROJECTITEMS"))
                {
                    return ExtractVsProjectItems(data, "CF_VSSTGPROJECTITEMS");
                }

                if (data.GetDataPresent("CF_VSREFPROJECTITEMS"))
                {
                    return ExtractVsProjectItems(data, "CF_VSREFPROJECTITEMS");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to extract VS project items: {ex.Message}");
            }

            return null;
        }

        private static string[] ExtractVsProjectItems(IDataObject data, string format)
        {
            // VS project items format contains:
            // - 4 bytes: count of items
            // - For each item: null-terminated project path, null-terminated item path
            var paths = new List<string>();

            try
            {
                using (var stream = data.GetData(format) as System.IO.MemoryStream)
                {
                    if (stream == null)
                        return null;

                    using (var reader = new System.IO.BinaryReader(stream))
                    {
                        // Skip the DROPFILES header (20 bytes on x86, varies on x64)
                        // The actual file paths follow as null-terminated strings
                        byte[] allBytes = reader.ReadBytes((int)stream.Length);
                        string allText = System.Text.Encoding.Unicode.GetString(allBytes);

                        // Split by null characters and filter valid file paths
                        string[] parts = allText.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string part in parts)
                        {
                            string trimmed = part.Trim();
                            // Check if it looks like a valid file path
                            if (!string.IsNullOrEmpty(trimmed) &&
                                (System.IO.File.Exists(trimmed) || System.IO.Directory.Exists(trimmed)))
                            {
                                paths.Add(trimmed);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error parsing VS project items: {ex.Message}");
            }

            return paths.Count > 0 ? paths.ToArray() : null;
        }

        #endregion
    }
}
