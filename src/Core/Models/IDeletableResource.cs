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
        /// Performs the delete operation.
        /// </summary>
        Task DeleteAsync();
    }
}
