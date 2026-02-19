using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Core.Search;

/// <summary>
/// Represents a single Azure resource search result with metadata for display and navigation.
/// </summary>
internal sealed class AzureSearchResult
{
    public AzureSearchResult(
        string resourceName,
        string resourceType,
        string resourceId,
        string subscriptionId,
        string subscriptionName,
        string accountId,
        string accountName,
        ImageMoniker iconMoniker)
    {
        ResourceName = resourceName;
        ResourceType = resourceType;
        ResourceId = resourceId;
        SubscriptionId = subscriptionId;
        SubscriptionName = subscriptionName;
        AccountId = accountId;
        AccountName = accountName;
        IconMoniker = iconMoniker;
    }

    /// <summary>
    /// The display name of the Azure resource.
    /// </summary>
    public string ResourceName { get; }

    /// <summary>
    /// The type of resource (e.g., "App Service", "Virtual Machine", "Storage Account").
    /// </summary>
    public string ResourceType { get; }

    /// <summary>
    /// The full Azure Resource Manager resource ID.
    /// </summary>
    public string ResourceId { get; }

    /// <summary>
    /// The subscription ID containing this resource.
    /// </summary>
    public string SubscriptionId { get; }

    /// <summary>
    /// The display name of the subscription containing this resource.
    /// </summary>
    public string SubscriptionName { get; }

    /// <summary>
    /// The account ID used to access this resource.
    /// </summary>
    public string AccountId { get; }

    /// <summary>
    /// The display name of the account (e.g., user email).
    /// </summary>
    public string AccountName { get; }

    /// <summary>
    /// The icon moniker for displaying the resource type.
    /// </summary>
    public ImageMoniker IconMoniker { get; }

    /// <summary>
    /// Gets a display string showing the resource path: Account > Subscription > Resource.
    /// </summary>
    public string DisplayPath => $"{AccountName} > {SubscriptionName}";

    public override string ToString() => $"{ResourceName} ({ResourceType})";
}
