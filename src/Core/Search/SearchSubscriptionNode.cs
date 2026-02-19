using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Core.Search;

/// <summary>
/// A lightweight subscription node used during search to maintain tree hierarchy.
/// Contains SearchResultNodes as children.
/// </summary>
internal sealed class SearchSubscriptionNode : ExplorerNodeBase
{
    public SearchSubscriptionNode(string subscriptionId, string subscriptionName)
        : base(subscriptionName)
    {
        SubscriptionId = subscriptionId;
        IsExpanded = true; // Auto-expand during search
        IsLoaded = true;   // No lazy loading needed
    }

    public string SubscriptionId { get; }

    public override ImageMoniker IconMoniker => KnownMonikers.AzureSubscriptionKey;
    public override int ContextMenuId => 0; // No context menu during search
    public override bool SupportsChildren => true;

    /// <summary>
    /// Adds a search result node to this subscription.
    /// </summary>
    public void AddResult(SearchResultNode resultNode)
    {
        Children.Add(resultNode);
    }
}
