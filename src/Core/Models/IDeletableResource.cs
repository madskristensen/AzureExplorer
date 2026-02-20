using System.Threading.Tasks;

namespace AzureExplorer.Core.Models
{
    /// <summary>
    /// Interface for nodes that support deletion.
    /// </summary>
    internal interface IDeletableResource
    {
        /// <summary>
        /// The display name of the resource type (e.g., "Secret", "Blob", "File").
        /// Used in confirmation dialogs.
        /// </summary>
        string DeleteResourceType { get; }

        /// <summary>
        /// The name of the resource being deleted.
        /// Used in confirmation dialogs.
        /// </summary>
        string DeleteResourceName { get; }

        /// <summary>
        /// The Azure resource provider type (e.g., "Microsoft.Storage/storageAccounts").
        /// Used to notify other views when the resource is deleted. Return null if not applicable.
        /// </summary>
        string DeleteResourceProvider { get; }

        /// <summary>
        /// The subscription ID containing this resource. Return null if not applicable.
        /// </summary>
        string DeleteSubscriptionId { get; }

        /// <summary>
        /// The resource group containing this resource. Return null if not applicable.
        /// </summary>
        string DeleteResourceGroupName { get; }

        /// <summary>
        /// Performs the delete operation.
        /// </summary>
        Task DeleteAsync();
    }
}
