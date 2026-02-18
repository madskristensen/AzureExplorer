using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azure.ResourceManager;
using Azure.ResourceManager.Cdn;
using Azure.ResourceManager.Resources;

using AzureExplorer.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Models
{
    /// <summary>
    /// Category node that lists all Azure Front Door profiles across the entire subscription.
    /// This allows users to see Front Doors they have direct access to, even if they
    /// don't have list permissions on the containing resource group.
    /// </summary>
    internal sealed class SubscriptionFrontDoorsNode : ExplorerNodeBase
    {
        public SubscriptionFrontDoorsNode(string subscriptionId)
            : base("Front Doors")
        {
            SubscriptionId = subscriptionId;
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }

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

                var frontDoors = new List<FrontDoorNode>();

                // Get Front Door profiles (Azure Front Door Standard/Premium) across subscription
                await foreach (ProfileResource profile in sub.GetProfilesAsync(cancellationToken: cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Extract resource group name from the resource ID
                    string resourceGroupName = profile.Id.ResourceGroupName;

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
                        resourceGroupName,
                        profile.Data.ResourceState?.ToString(),
                        hostName));
                }

                // Sort alphabetically by name
                foreach (var node in frontDoors.OrderBy(f => f.Label, StringComparer.OrdinalIgnoreCase))
                {
                    AddChild(node);
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
    }
}
