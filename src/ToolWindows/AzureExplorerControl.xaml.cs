using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

using AzureExplorer.AppService.Models;
using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell.Interop;

namespace AzureExplorer.ToolWindows
{
    public partial class AzureExplorerControl : UserControl
    {
        private static AzureExplorerControl _instance;

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
        /// Configures the TreeView entirely in code to avoid XAML assembly resolution issues.
        /// </summary>
        private void SetupTreeView()
        {
            // Data template: StackPanel with Icon + Label + Description text
            var stackFactory = new FrameworkElementFactory(typeof(StackPanel));
            stackFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            stackFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 2, 0, 2));

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

            var accounts = AzureAuthService.Instance.Accounts;
            if (accounts.Count == 0)
            {
                RootNodes.Add(new SignInNode());
                EmptyMessage.Visibility = Visibility.Collapsed;
                return;
            }

            EmptyMessage.Visibility = Visibility.Collapsed;

            // Create an AccountNode for each signed-in account
            foreach (var account in accounts.OrderBy(a => a.Username, StringComparer.OrdinalIgnoreCase))
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
    }
}
