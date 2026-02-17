using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Models
{
    /// <summary>
    /// Abstract base class for all tree nodes in the Azure Explorer tool window.
    /// Provides common properties for display, lazy-loading children, and VSCT context menu integration.
    /// </summary>
    internal abstract class ExplorerNodeBase : INotifyPropertyChanged
    {
        private string _label;
        private string _description;
        private bool _isExpanded;
        private bool _isLoading;
        private bool _isLoaded;

        protected ExplorerNodeBase(string label)
        {
            _label = label;
            Children = new ObservableCollection<ExplorerNodeBase>();
        }

        public string Label
        {
            get => _label;
            set => SetProperty(ref _label, value);
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

        public ObservableCollection<ExplorerNodeBase> Children { get; }

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
        public virtual Task LoadChildrenAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Resets loaded state and reloads children.
        /// </summary>
        public async Task RefreshAsync()
        {
            IsLoaded = false;
            IsLoading = false;
            Description = null;
            Children.Clear();
            await LoadChildrenAsync();
        }

        /// <summary>
        /// Marks this node as loading. Shows inline "loading..." text on this node
        /// and removes any <see cref="LoadingNode"/> placeholder children.
        /// </summary>
        protected bool BeginLoading()
        {
            if (IsLoaded || IsLoading)
                return false;

            IsLoading = true;
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
        /// Adds a child node.
        /// </summary>
        protected void AddChild(ExplorerNodeBase child)
        {
            child.Parent = this;
            Children.Add(child);
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
