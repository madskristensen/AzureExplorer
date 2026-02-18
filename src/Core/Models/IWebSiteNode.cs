namespace AzureExplorer.Core.Models
{
    /// <summary>
    /// Interface for Azure Web Sites (App Services and Function Apps) that share
    /// common properties and operations like browsing, Kudu, start/stop, etc.
    /// </summary>
    internal interface IWebSiteNode
    {
        string SubscriptionId { get; }
        string ResourceGroupName { get; }
        string Label { get; }
        string BrowseUrl { get; }
        string DefaultHostName { get; }
    }
}
