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

    /// <summary>
    /// Returns the underlying actual node so commands can check its type and perform operations.
    /// </summary>
    public override ExplorerNodeBase ActualNode => _actualNode ?? this;

    public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
    {
        if (_actualNode == null)
            return;

        if (!BeginLoading())
            return;

        try
        {
            // Load children from the actual node
            await _actualNode.LoadChildrenAsync(cancellationToken);

            // Switch to UI thread before modifying WPF-bound ObservableCollection
            await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Copy all children to this node (including FilesNode)
            // Files are not searched, but users can expand the FilesNode to browse
            foreach (ExplorerNodeBase child in _actualNode.Children)
            {
                // Set parent reference for proper tree navigation
                child.Parent = this;
                Children.Add(child);
            }
        }
        catch (Exception ex)
        {
            await ex.LogAsync();
            Children.Add(new LoadingNode { Label = $"Error: {ex.Message}" });
        }
        finally
        {
            EndLoading();
        }
    }
}
