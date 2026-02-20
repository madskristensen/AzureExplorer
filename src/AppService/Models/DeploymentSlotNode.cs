using System.Collections.Generic;

using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.AppService.Models
{
    /// <summary>
    /// Represents an Azure App Service deployment slot. Deployment slots are live apps with their own
    /// hostnames that can be swapped with production for zero-downtime deployments.
    /// </summary>
    internal sealed class DeploymentSlotNode : WebSiteNodeBase
    {
        /// <summary>
        /// Creates a new deployment slot node.
        /// </summary>
        /// <param name="slotName">The name of the slot (e.g., "staging", "dev").</param>
        /// <param name="appServiceName">The parent App Service name.</param>
        /// <param name="subscriptionId">The subscription ID.</param>
        /// <param name="resourceGroupName">The resource group name.</param>
        /// <param name="state">The current state of the slot (Running/Stopped).</param>
        /// <param name="defaultHostName">The default hostname for the slot.</param>
        /// <param name="tags">Optional tags on the slot.</param>
        public DeploymentSlotNode(
            string slotName,
            string appServiceName,
            string subscriptionId,
            string resourceGroupName,
            string state,
            string defaultHostName,
            IDictionary<string, string> tags = null)
            : base(slotName, subscriptionId, resourceGroupName, state, defaultHostName, tags)
        {
            AppServiceName = appServiceName;
            SlotName = slotName;

            // Clear the loading placeholder - slots don't have children
            Children.Clear();
        }

        /// <summary>
        /// Gets the parent App Service name.
        /// </summary>
        public string AppServiceName { get; }

        /// <summary>
        /// Gets the slot name.
        /// </summary>
        public string SlotName { get; }

        /// <summary>
        /// Gets the context menu ID for deployment slots.
        /// </summary>
        public override int ContextMenuId => PackageIds.DeploymentSlotContextMenu;

        /// <summary>
        /// Deployment slots don't have expandable children.
        /// </summary>
        public override bool SupportsChildren => false;

        /// <summary>
        /// Gets the icon for running slots - uses a slot-specific icon.
        /// </summary>
        protected override ImageMoniker RunningIconMoniker => KnownMonikers.CloudService;
    }
}
