using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azure.ResourceManager;
using Azure.ResourceManager.Cdn;
using Azure.ResourceManager.Resources;

using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.FrontDoor.Models
{
    /// <summary>
    /// Category node that groups Azure Front Door profiles under a resource group.
    /// </summary>
    internal sealed class FrontDoorsNode : ExplorerNodeBase
    {
        public FrontDoorsNode(string subscriptionId, string resourceGroupName)
            : base("Front Doors")
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }

        public override ImageMoniker IconMoniker => KnownMonikers.CloudGroup;
        public override int ContextMenuId => PackageIds.FrontDoorsCategoryContextMenu;
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

                var frontDoors = new List<FrontDoorNode>();

                // Get Front Door profiles (Azure Front Door Standard/Premium)
                await foreach (ProfileResource profile in rg.GetProfiles().GetAllAsync(cancellationToken: cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Get the first endpoint hostname if available
                    string hostName = null;
                    try
                    {
                        await foreach (FrontDoorEndpointResource endpoint in profile.GetFrontDoorEndpoints().GetAllAsync(cancellationToken: cancellationToken))
                        {
                            hostName = endpoint.Data.HostName;
                            break;
                        }
                    }
                    catch
                    {
                        // Endpoint retrieval may fail; continue without hostname
                    }

                    frontDoors.Add(new FrontDoorNode(
                        profile.Data.Name,
                        SubscriptionId,
                        ResourceGroupName,
                        profile.Data.ResourceState?.ToString(),
                        hostName));
                }

                // Sort alphabetically by name
                foreach (FrontDoorNode node in frontDoors.OrderBy(f => f.Label, StringComparer.OrdinalIgnoreCase))
                {
                    AddChild(node);
                }
            }
            catch (Exception ex)
            {
                if (Children.Count <= 1)
                {
                    Children.Clear();
                    Children.Add(new LoadingNode { Label = $"Error: {ex.Message}" });
                    IsLoading = false;
                    IsLoaded = true;
                    return;
                }
            }

            EndLoading();
        }
    }
}
