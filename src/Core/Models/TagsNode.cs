using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Core.Models
{
    /// <summary>
    /// Container node that shows "Tags (n)" and expands to show individual tag key-value pairs.
    /// </summary>
    internal sealed class TagsNode : ExplorerNodeBase
    {
        private readonly IReadOnlyDictionary<string, string> _tags;

        public TagsNode(IReadOnlyDictionary<string, string> tags)
            : base($"Tags ({tags?.Count ?? 0})")
        {
            _tags = tags ?? new Dictionary<string, string>();

            // Add placeholder for expansion if there are tags
            if (_tags.Count > 0)
            {
                Children.Add(new LoadingNode());
            }
        }

        public override ImageMoniker IconMoniker => KnownMonikers.Bookmark;

        public override int ContextMenuId => PackageIds.TagsNodeContextMenu;

        public override bool SupportsChildren => _tags.Count > 0;

        /// <summary>
        /// Gets the tags dictionary for copy operations.
        /// </summary>
        public IReadOnlyDictionary<string, string> Tags => _tags;

        public override Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return Task.CompletedTask;

            Children.Clear();

            foreach (KeyValuePair<string, string> tag in _tags.OrderBy(t => t.Key, StringComparer.OrdinalIgnoreCase))
            {
                AddChild(new TagNode(tag.Key, tag.Value));
            }

            EndLoading();
            return Task.CompletedTask;
        }
    }
}
