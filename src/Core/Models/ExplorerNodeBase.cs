using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Core.Models
{
    /// <summary>
    /// Abstract base class for all tree nodes in the Azure Explorer tool window.
    /// Provides common properties for display, lazy-loading children, and VSCT context menu integration.
    /// </summary>
    internal abstract class ExplorerNodeBase(string label) : INotifyPropertyChanged
    {
        private string _description;
        private bool _isExpanded;
        private bool _isLoading;
        private bool _isLoaded;
        private readonly object _loadingLock = new();

        public string Label
        {
            get => label;
            set => SetProperty(ref label, value);
        }

        /// <summary>
        /// Secondary text shown as a tooltip or status indicator.
        /// </summary>
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        /// <summary>
        /// The KnownMonikers icon for this node.
        /// </summary>
        public abstract ImageMoniker IconMoniker { get; }

        /// <summary>
        /// The VSCT context menu ID to show on right-click. Return 0 for no context menu.
        /// </summary>
        public abstract int ContextMenuId { get; }

        /// <summary>
        /// Whether this node type supports child nodes and should show an expand arrow.
        /// </summary>
        public abstract bool SupportsChildren { get; }

        /// <summary>
        /// Gets whether this node should be visible in the tree view.
        /// Override to implement custom visibility logic (e.g., hidden subscriptions/tenants).
        /// </summary>
        public virtual bool IsVisible => true;

        /// <summary>
        /// Gets the opacity for this node in the tree view. Default is 1.0 (fully visible).
        /// Override to return a lower value (e.g., 0.5) for dimmed nodes like hidden subscriptions.
        /// </summary>
        public virtual double Opacity => 1.0;

        /// <summary>
        /// Gets the actual node for command operations. For wrapper nodes like SearchResultNode,
        /// this returns the underlying wrapped node. For regular nodes, returns this instance.
        /// Commands should use this property when checking node types or performing operations.
        /// </summary>
        public virtual ExplorerNodeBase ActualNode => this;

        public ObservableCollection<ExplorerNodeBase> Children { get; } = [];

        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsLoaded
        {
            get => _isLoaded;
            set => SetProperty(ref _isLoaded, value);
        }

        public ExplorerNodeBase Parent { get; set; }

        /// <summary>
        /// Override to load child nodes asynchronously. Called once when the node is first expanded.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the loading operation.</param>
        public virtual Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Resets loaded state and reloads children with timeout protection.
        /// </summary>
        public async Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            IsLoaded = false;
            IsLoading = false;
            Description = null;
            Children.Clear();

            try
            {
                await AsyncHelper.WithTimeoutAsync(
                    ct => LoadChildrenAsync(ct),
                    cancellationToken: cancellationToken);
            }
            catch (TimeoutException)
            {
                Description = "Timed out - check connection";
                EndLoading();
            }
        }

        /// <summary>
        /// Marks this node as loading. Shows inline "loading..." text on this node
        /// and removes any <see cref="LoadingNode"/> placeholder children.
        /// Thread-safe: prevents concurrent loading attempts.
        /// </summary>
        protected bool BeginLoading()
        {
            lock (_loadingLock)
            {
                if (IsLoaded || IsLoading)
                    return false;

                IsLoading = true;
            }

            Description = "loading...";

            for (var i = Children.Count - 1; i >= 0; i--)
            {
                if (Children[i] is LoadingNode)
                    Children.RemoveAt(i);
            }

            return true;
        }

        /// <summary>
        /// Clears the inline loading text and marks loading complete.
        /// </summary>
        protected void EndLoading()
        {
            Description = null;
            IsLoading = false;
            IsLoaded = true;
        }

        /// <summary>
        /// Helper method that wraps the common loading pattern with standardized error handling.
        /// Call <see cref="BeginLoading"/> before this method if needed.
        /// </summary>
        /// <param name="loadAction">The action that loads child nodes.</param>
        /// <param name="cancellationToken">Token to cancel the loading operation.</param>
        protected async Task LoadChildrenWithErrorHandlingAsync(Func<CancellationToken, Task> loadAction, CancellationToken cancellationToken = default)
        {
            try
            {
                await loadAction(cancellationToken);
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                Children.Clear();
                Children.Add(new LoadingNode { Label = $"Error: {ex.Message}" });
            }
            finally
            {
                EndLoading();
            }
        }

        /// <summary>
        /// Adds a child node.
        /// </summary>
        protected void AddChild(ExplorerNodeBase child)
        {
            child.Parent = this;
            Children.Add(child);
        }

        /// <summary>
        /// Inserts a child node in alphabetically sorted order by label.
        /// </summary>
        protected void InsertChildSorted(ExplorerNodeBase node)
        {
            node.Parent = this;

            var index = 0;
            while (index < Children.Count &&
                   string.Compare(Children[index].Label, node.Label, StringComparison.OrdinalIgnoreCase) < 0)
            {
                index++;
            }

            Children.Insert(index, node);
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
