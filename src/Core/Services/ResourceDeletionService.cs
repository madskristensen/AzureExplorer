using System;

namespace AzureExplorer.Core.Services
{
    /// <summary>
    /// Service for broadcasting resource lifecycle events across the tree.
    /// When a resource is created or deleted from one view (e.g., resource group-level),
    /// other views (e.g., subscription-level) can listen and update accordingly.
    /// </summary>
    internal static class ResourceNotificationService
    {
        /// <summary>
        /// Event raised when a resource is created. Subscribers should check
        /// if they should display the new resource and add it to their children.
        /// </summary>
        public static event EventHandler<ResourceCreatedEventArgs> ResourceCreated;

        /// <summary>
        /// Event raised when a resource is deleted. Subscribers should check
        /// if they contain a matching resource and remove it from their children.
        /// </summary>
        public static event EventHandler<ResourceDeletedEventArgs> ResourceDeleted;

        /// <summary>
        /// Notifies all subscribers that a resource has been created.
        /// </summary>
        public static void NotifyCreated(string resourceType, string subscriptionId, string resourceGroupName, string resourceName, object additionalData = null)
        {
            ResourceCreated?.Invoke(null, new ResourceCreatedEventArgs(resourceType, subscriptionId, resourceGroupName, resourceName, additionalData));
        }

        /// <summary>
        /// Notifies all subscribers that a resource has been deleted.
        /// </summary>
        public static void NotifyDeleted(string resourceType, string subscriptionId, string resourceGroupName, string resourceName)
        {
            ResourceDeleted?.Invoke(null, new ResourceDeletedEventArgs(resourceType, subscriptionId, resourceGroupName, resourceName));
        }
    }

    /// <summary>
    /// Event args for resource creation notifications.
    /// </summary>
    internal sealed class ResourceCreatedEventArgs : EventArgs
    {
        public ResourceCreatedEventArgs(string resourceType, string subscriptionId, string resourceGroupName, string resourceName, object additionalData)
        {
            ResourceType = resourceType;
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            ResourceName = resourceName;
            AdditionalData = additionalData;
        }

        public string ResourceType { get; }
        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }
        public string ResourceName { get; }

        /// <summary>
        /// Optional additional data needed to create the node (e.g., SKU name for storage accounts).
        /// </summary>
        public object AdditionalData { get; }
    }

    /// <summary>
    /// Event args for resource deletion notifications.
    /// </summary>
    internal sealed class ResourceDeletedEventArgs : EventArgs
    {
        public ResourceDeletedEventArgs(string resourceType, string subscriptionId, string resourceGroupName, string resourceName)
        {
            ResourceType = resourceType;
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            ResourceName = resourceName;
        }

        public string ResourceType { get; }
        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }
        public string ResourceName { get; }
    }
}
