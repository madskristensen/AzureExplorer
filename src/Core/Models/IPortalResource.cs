namespace AzureExplorer.Core.Models
{
    /// <summary>
    /// Interface for Azure resources that can be opened in the Azure Portal.
    /// </summary>
    internal interface IPortalResource
    {
        /// <summary>
        /// The Azure subscription ID containing this resource.
        /// </summary>
        string SubscriptionId { get; }

        /// <summary>
        /// The resource group name containing this resource. Null for subscriptions.
        /// </summary>
        string ResourceGroupName { get; }

        /// <summary>
        /// The name of the resource (typically the Label of the node). Null for resource groups.
        /// </summary>
        string ResourceName { get; }

        /// <summary>
        /// The Azure Resource Provider path (e.g., "Microsoft.Web/sites", "Microsoft.KeyVault/vaults").
        /// Null for subscriptions and resource groups.
        /// </summary>
        string AzureResourceProvider { get; }
    }

    /// <summary>
    /// Extension methods for <see cref="IPortalResource"/>.
    /// </summary>
    internal static class PortalResourceExtensions
    {
        /// <summary>
        /// Gets the Azure Portal URL for this resource.
        /// </summary>
        public static string GetPortalUrl(this IPortalResource resource)
        {
            // Subscription only: https://portal.azure.com/#@/resource/subscriptions/{sub}/overview
            if (string.IsNullOrEmpty(resource.ResourceGroupName))
            {
                return $"https://portal.azure.com/#@/resource/subscriptions/{resource.SubscriptionId}/overview";
            }

            // Resource group: https://portal.azure.com/#@/resource/subscriptions/{sub}/resourceGroups/{rg}/overview
            if (string.IsNullOrEmpty(resource.AzureResourceProvider))
            {
                return $"https://portal.azure.com/#@/resource/subscriptions/{resource.SubscriptionId}" +
                       $"/resourceGroups/{resource.ResourceGroupName}/overview";
            }

            // Full resource: https://portal.azure.com/#@/resource/subscriptions/{sub}/resourceGroups/{rg}/providers/{provider}/{name}/overview
            return $"https://portal.azure.com/#@/resource/subscriptions/{resource.SubscriptionId}" +
                   $"/resourceGroups/{resource.ResourceGroupName}" +
                   $"/providers/{resource.AzureResourceProvider}/{resource.ResourceName}/overview";
        }
    }
}
