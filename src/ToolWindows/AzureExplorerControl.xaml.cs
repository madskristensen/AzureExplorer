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
using AzureExplorer.Core.Search;
using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell.Interop;

namespace AzureExplorer.ToolWindows
{
    public partial class AzureExplorerControl : UserControl
    {
        private static AzureExplorerControl _instance;
        private readonly List<ExplorerNodeBase> _savedRootNodes = [];
        private bool _isSearchActive;

        public AzureExplorerControl()
        {
            InitializeComponent();
            _instance = this;

            RootNodes = [];

            SetupTreeView();

            AzureAuthService.Instance.AuthStateChanged += OnAuthStateChanged;
            Unloaded += OnUnloaded;

            RefreshRootNodes();

            // Attempt to restore a previous session silently (no browser popup)
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await AzureAuthService.Instance.TrySilentSignInAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Silent sign-in failed: {ex.Message}");
                }
            }).FireAndForget();
        }

        internal ObservableCollection<ExplorerNodeBase> RootNodes { get; }

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
            // Data template: StackPanel with Icon + Label + Description text
            var stackFactory = new FrameworkElementFactory(typeof(StackPanel));
            stackFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            stackFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 2, 0, 2));
            stackFactory.SetBinding(UIElement.OpacityProperty, new Binding("Opacity"));

            var imageFactory = new FrameworkElementFactory(typeof(CrispImage));
            imageFactory.SetBinding(CrispImage.MonikerProperty, new Binding("IconMoniker"));
            imageFactory.SetValue(FrameworkElement.WidthProperty, 16.0);
            imageFactory.SetValue(FrameworkElement.HeightProperty, 16.0);
            imageFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 4, 0));
            imageFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            stackFactory.AppendChild(imageFactory);

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

            // ItemContainerStyle: Bind TreeViewItem.Visibility to IsVisible property
            // This allows nodes to stay in the tree but be hidden/shown without reload
            // BasedOn ensures we inherit the VS theme colors
            var baseStyle = (Style)ExplorerTree.FindResource(typeof(TreeViewItem));
            var itemContainerStyle = new Style(typeof(TreeViewItem), baseStyle);
            itemContainerStyle.Setters.Add(new Setter(
                TreeViewItem.VisibilityProperty,
                new Binding("IsVisible") { Converter = new BooleanToVisibilityConverter() }));
            ExplorerTree.ItemContainerStyle = itemContainerStyle;

            // Bind the collection
            ExplorerTree.ItemsSource = RootNodes;

            // Wire events
            ExplorerTree.AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(TreeViewItem_Expanded));
            ExplorerTree.PreviewMouseRightButtonDown += ExplorerTree_PreviewMouseRightButtonDown;
            ExplorerTree.PreviewMouseRightButtonUp += ExplorerTree_PreviewMouseRightButtonUp;
            ExplorerTree.MouseDoubleClick += ExplorerTree_MouseDoubleClick;
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

                // Load children first, then expand
                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    try
                    {
                        await accountNode.LoadChildrenAsync();
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        accountNode.IsExpanded = true;
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
                e.Handled = true;
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
    }
}
