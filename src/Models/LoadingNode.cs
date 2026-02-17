using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Models
{
    /// <summary>
    /// Placeholder node shown while a parent node is loading its children.
    /// </summary>
    internal sealed class LoadingNode : ExplorerNodeBase
    {
        public LoadingNode() : base("Loading...") { }

        public override ImageMoniker IconMoniker => KnownMonikers.Loading;
        public override int ContextMenuId => 0;
        public override bool SupportsChildren => false;
    }
}
