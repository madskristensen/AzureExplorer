using System.Text.Json;
using System.Threading;

using Azure.ResourceManager;
using Azure.ResourceManager.Resources;

using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.VirtualMachine.Models
{
    /// <summary>
    /// Category node that groups Azure Virtual Machines under a resource group.
    /// </summary>
    internal sealed class VirtualMachinesNode : ExplorerNodeBase
    {
        public VirtualMachinesNode(string subscriptionId, string resourceGroupName)
            : base("Virtual Machines")
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }

        public override ImageMoniker IconMoniker => KnownMonikers.AzureVirtualMachine;
        public override int ContextMenuId => PackageIds.VirtualMachinesCategoryContextMenu;
        public override bool SupportsChildren => true;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            try
            {
                ArmClient client = AzureResourceService.Instance.GetClient(SubscriptionId);
                SubscriptionResource sub = client.GetSubscriptionResource(
                    SubscriptionResource.CreateResourceIdentifier(SubscriptionId));
                ResourceGroupResource rg = (await sub.GetResourceGroupAsync(ResourceGroupName, cancellationToken)).Value;

                // Query VMs in this resource group
                var filter = "resourceType eq 'Microsoft.Compute/virtualMachines'";
                await foreach (GenericResource resource in rg.GetGenericResourcesAsync(filter: filter, expand: "properties", cancellationToken: cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var name = resource.Data.Name;
                    string vmSize = null;
                    string osType = null;

                    if (resource.Data.Properties != null)
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(resource.Data.Properties);
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

                    var node = new VirtualMachineNode(
                        name,
                        SubscriptionId,
                        ResourceGroupName,
                        state: null,
                        vmSize,
                        osType,
                        publicIpAddress: null,
                        privateIpAddress: null);

                    InsertChildSorted(node);
                }
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                Children.Clear();
                Children.Add(new LoadingNode { Label = $"Error: {ex.Message}" });
            }
            finally
            {
                EndLoading();
            }
        }

        /// <summary>
        /// Inserts a child node in alphabetically sorted order by label.
        /// </summary>
        private void InsertChildSorted(ExplorerNodeBase node)
        {
            var index = 0;
            while (index < Children.Count &&
                   string.Compare(Children[index].Label, node.Label, StringComparison.OrdinalIgnoreCase) < 0)
            {
                index++;
            }
            Children.Insert(index, node);
        }
    }
}
