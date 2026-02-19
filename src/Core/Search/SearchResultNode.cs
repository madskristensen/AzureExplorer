using System.Threading;

using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Core.Search;

/// <summary>
/// A tree node representing a search result. Displays as "ResourceName (ResourceType)".
/// The hierarchy (Account > Subscription) is maintained by parent nodes.
/// Can optionally hold a reference to load children from the actual resource.
/// </summary>
internal sealed class SearchResultNode : ExplorerNodeBase
{
    private readonly ExplorerNodeBase _actualNode;

    public SearchResultNode(
        string resourceName,
        string resourceType,
        string resourceId,
        string subscriptionId,
        string subscriptionName,
        string accountName,
        ImageMoniker iconMoniker,
        ExplorerNodeBase actualNode = null)
        : base($"{resourceName} ({resourceType})")
    {
        ResourceName = resourceName;
        ResourceType = resourceType;
        ResourceId = resourceId;
        SubscriptionId = subscriptionId;
        SubscriptionName = subscriptionName;
        AccountName = accountName;
        IconMoniker = iconMoniker;
        _actualNode = actualNode;

        // No description needed - path is visible in tree hierarchy

        // Add loading placeholder if we have an actual node that supports children
        if (_actualNode != null && _actualNode.SupportsChildren)
        {
            Children.Add(new LoadingNode());
        }
    }

    public string ResourceName { get; }
    public string ResourceType { get; }
    public string ResourceId { get; }
    public string SubscriptionId { get; }
    public string SubscriptionName { get; }
    public string AccountName { get; }

    public override ImageMoniker IconMoniker { get; }

    // Use the actual node's context menu if available
    public override int ContextMenuId => _actualNode?.ContextMenuId ?? 0;

    public override bool SupportsChildren => _actualNode?.SupportsChildren ?? false;

    public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
    {
        if (_actualNode == null)
            return;

        // Load children from the actual node
        await _actualNode.LoadChildrenAsync(cancellationToken);

        // Copy children to this node, excluding FilesNode to avoid deep loading
        Children.Clear();
        foreach (ExplorerNodeBase child in _actualNode.Children)
        {
            // Skip Files nodes to avoid loading file trees
            if (IsFilesNode(child))
                continue;

            Children.Add(child);
        }

        IsLoaded = true;
    }

    private static bool IsFilesNode(ExplorerNodeBase node)
    {
        // Check by type name to avoid coupling to specific namespace
        var typeName = node.GetType().Name;
        return typeName == "FilesNode";
    }
}
