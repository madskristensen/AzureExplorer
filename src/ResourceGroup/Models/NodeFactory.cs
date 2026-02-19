using System;
using System.Collections.Generic;
using System.Text.Json;

using Azure.ResourceManager.Resources;

using AzureExplorer.AppService.Models;
using AzureExplorer.AppServicePlan.Models;
using AzureExplorer.Core.Models;
using AzureExplorer.FrontDoor.Models;
using AzureExplorer.FunctionApp.Models;
using AzureExplorer.KeyVault.Models;
using AzureExplorer.Sql.Models;
using AzureExplorer.Storage.Models;
using AzureExplorer.VirtualMachine.Models;

namespace AzureExplorer.ResourceGroup.Models
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
                        ["Microsoft.Web/serverfarms"] = CreateAppServicePlanNode,
                        ["Microsoft.Cdn/profiles"] = CreateFrontDoorNode,
                        ["Microsoft.KeyVault/vaults"] = CreateKeyVaultNode,
                        ["Microsoft.Storage/storageAccounts"] = CreateStorageAccountNode,
                        ["Microsoft.Sql/servers"] = CreateSqlServerNode,
                        ["Microsoft.Compute/virtualMachines"] = CreateVirtualMachineNode,
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
            // Check if this is a Function App based on the kind property
            if (FunctionAppNode.IsFunctionApp(resource.Kind))
            {
                return new FunctionAppNode(
                    resource.Name,
                    subscriptionId,
                    resourceGroupName,
                    state: null,
                    defaultHostName: null,
                    tags: resource.Tags);
            }

            // Regular App Service (Web App)
            return new AppServiceNode(
                resource.Name,
                subscriptionId,
                resourceGroupName,
                state: null,
                defaultHostName: null,
                tags: resource.Tags);
        }

        private static ExplorerNodeBase CreateAppServicePlanNode(GenericResourceData resource, string subscriptionId, string resourceGroupName)
        {
            // GenericResourceData doesn't carry plan-specific details, so we use minimal info.
            // The ResourceGroupNode.LoadChildrenAsync uses AppService-specific APIs for full details.
            return new AppServicePlanNode(
                resource.Name,
                subscriptionId,
                resourceGroupName,
                sku: null,
                kind: null,
                numberOfSites: null);
        }

        private static ExplorerNodeBase CreateFrontDoorNode(GenericResourceData resource, string subscriptionId, string resourceGroupName)
        {
            // GenericResourceData doesn't carry Front Door-specific details.
            // The FrontDoorsNode.LoadChildrenAsync uses CDN-specific APIs for full details.
            return new FrontDoorNode(
                resource.Name,
                subscriptionId,
                resourceGroupName,
                state: null,
                hostName: null);
        }

        private static ExplorerNodeBase CreateKeyVaultNode(GenericResourceData resource, string subscriptionId, string resourceGroupName)
        {
            // GenericResourceData doesn't carry Key Vault-specific details.
            // The KeyVaultsNode.LoadChildrenAsync uses KeyVault-specific APIs for full details.
            return new KeyVaultNode(
                resource.Name,
                subscriptionId,
                resourceGroupName,
                state: null,
                vaultUri: null,
                tags: resource.Tags);
        }

        private static ExplorerNodeBase CreateStorageAccountNode(GenericResourceData resource, string subscriptionId, string resourceGroupName)
        {
            // GenericResourceData doesn't carry Storage Account-specific details.
            // The StorageAccountsNode.LoadChildrenAsync uses Storage-specific APIs for full details.
            return new StorageAccountNode(
                resource.Name,
                subscriptionId,
                resourceGroupName,
                state: null,
                kind: null,
                skuName: null,
                tags: resource.Tags);
        }

        private static ExplorerNodeBase CreateSqlServerNode(GenericResourceData resource, string subscriptionId, string resourceGroupName)
        {
            // GenericResourceData doesn't carry SQL Server-specific details.
            // The SqlServersNode.LoadChildrenAsync uses Sql-specific APIs for full details.
            return new SqlServerNode(
                resource.Name,
                subscriptionId,
                resourceGroupName,
                state: null,
                fullyQualifiedDomainName: null,
                tags: resource.Tags);
        }

        private static ExplorerNodeBase CreateVirtualMachineNode(GenericResourceData resource, string subscriptionId, string resourceGroupName)
        {
            string vmSize = null;
            string osType = null;

            if (resource.Properties != null)
            {
                try
                {
                    using var doc = JsonDocument.Parse(resource.Properties);
                    JsonElement root = doc.RootElement;

                    if (root.TryGetProperty("hardwareProfile", out JsonElement hardwareProfile) &&
                        hardwareProfile.TryGetProperty("vmSize", out JsonElement sizeElement))
                    {
                        vmSize = sizeElement.GetString();
                    }

                    if (root.TryGetProperty("storageProfile", out JsonElement storageProfile) &&
                        storageProfile.TryGetProperty("osDisk", out JsonElement osDisk) &&
                        osDisk.TryGetProperty("osType", out JsonElement osTypeElement))
                    {
                        osType = osTypeElement.GetString();
                    }
                }
                catch
                {
                    // Properties parsing failed; continue with null values
                }
            }

            return new VirtualMachineNode(
                resource.Name,
                subscriptionId,
                resourceGroupName,
                state: null,
                vmSize,
                osType,
                publicIpAddress: null,
                privateIpAddress: null,
                tags: resource.Tags);
        }
    }
}
