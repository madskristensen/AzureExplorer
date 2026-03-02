using System.Collections.Generic;

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
    private readonly Dictionary<string, SearchSubscriptionNode> _subscriptionsById =
        new(StringComparer.OrdinalIgnoreCase);

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
        if (_subscriptionsById.TryGetValue(subscriptionId, out SearchSubscriptionNode existingNode))
            return existingNode;

        // Create new subscription node
        var newSubNode = new SearchSubscriptionNode(subscriptionId, subscriptionName);
        _subscriptionsById[subscriptionId] = newSubNode;
        AddChild(newSubNode);

        return newSubNode;
    }
}
