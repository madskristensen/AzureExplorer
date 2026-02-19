using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Core.Search;

/// <summary>
/// A lightweight account node used during search to maintain tree hierarchy.
/// Contains SearchSubscriptionNodes as children.
/// </summary>
internal sealed class SearchAccountNode : ExplorerNodeBase
{
    public SearchAccountNode(string accountId, string accountName)
        : base(accountName)
    {
        AccountId = accountId;
        IsExpanded = true; // Auto-expand during search
        IsLoaded = true;   // No lazy loading needed
    }

    public string AccountId { get; }

    public override ImageMoniker IconMoniker => KnownMonikers.Cloud;
    public override int ContextMenuId => 0; // No context menu during search
    public override bool SupportsChildren => true;

    /// <summary>
    /// Gets or creates a subscription node for the given subscription.
    /// </summary>
    public SearchSubscriptionNode GetOrCreateSubscription(string subscriptionId, string subscriptionName)
    {
        // Check if we already have this subscription
        foreach (ExplorerNodeBase child in Children)
        {
            if (child is SearchSubscriptionNode subNode && subNode.SubscriptionId == subscriptionId)
            {
                return subNode;
            }
        }

        // Create new subscription node
        var newSubNode = new SearchSubscriptionNode(subscriptionId, subscriptionName);
        Children.Add(newSubNode);
        return newSubNode;
    }
}
