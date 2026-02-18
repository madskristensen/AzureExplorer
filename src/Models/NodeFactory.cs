using System;
using System.Collections.Generic;

using Azure.ResourceManager.Resources;

namespace AzureExplorer.Models
{
    /// <summary>
    /// Maps Azure resource types to tree node constructors. Register new resource types here
    /// to extend the explorer with additional node kinds.
    /// </summary>
    internal static class NodeFactory
    {
        private static readonly Dictionary<string, Func<GenericResourceData, string, string, ExplorerNodeBase>> _creators
            = new(StringComparer.OrdinalIgnoreCase)
            {
                ["Microsoft.Web/sites"] = CreateAppServiceNode,
            };

        /// <summary>
        /// Creates the appropriate tree node for the given Azure resource, or null if the resource type is not supported.
        /// </summary>
        public static ExplorerNodeBase Create(GenericResourceData resource, string subscriptionId, string resourceGroupName)
        {
            if (resource?.ResourceType == null)
                return null;

            var resourceType = resource.ResourceType.ToString();
            if (_creators.TryGetValue(resourceType, out Func<GenericResourceData, string, string, ExplorerNodeBase> creator))
            {
                return creator(resource, subscriptionId, resourceGroupName);
            }

            return null;
        }

        /// <summary>
        /// Registers a new resource type to node mapping. Use this to extend the explorer with new resource types.
        /// </summary>
        public static void Register(string azureResourceType, Func<GenericResourceData, string, string, ExplorerNodeBase> creator)
        {
            _creators[azureResourceType] = creator;
        }

        private static ExplorerNodeBase CreateAppServiceNode(GenericResourceData resource, string subscriptionId, string resourceGroupName)
        {
            // GenericResourceData doesn't carry app-specific state, so we default to Unknown.
            // The ResourceGroupNode.LoadChildrenAsync will be enhanced later to use AppService-specific APIs.
            return new AppServiceNode(
                resource.Name,
                subscriptionId,
                resourceGroupName,
                state: null,
                defaultHostName: null);
        }
    }
}
