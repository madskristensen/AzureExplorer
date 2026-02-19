using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Core.Models
{
    /// <summary>
    /// Represents a single tag key-value pair in the tree.
    /// </summary>
    internal sealed class TagNode : ExplorerNodeBase
    {
        public TagNode(string key, string value)
            : base(key)
        {
            Key = key;
            Value = value;
            Description = value;  // Shows "Environment  Production" style in tree
        }

        /// <summary>
        /// The tag key (name).
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// The tag value.
        /// </summary>
        public string Value { get; }

        public override ImageMoniker IconMoniker => KnownMonikers.Bookmark;

        public override int ContextMenuId => PackageIds.TagContextMenu;

        public override bool SupportsChildren => false;
    }
}
