using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azure.ResourceManager.Resources;

using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.ResourceGroup.Models
{
    /// <summary>
    /// Category node that groups all resource groups under a subscription.
    /// </summary>
    internal sealed class ResourceGroupsNode : ExplorerNodeBase
    {
        public ResourceGroupsNode(string subscriptionId)
            : base("Resource Groups")
        {
            SubscriptionId = subscriptionId;
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }

        public override ImageMoniker IconMoniker => KnownMonikers.AzureResourceGroup;
        public override int ContextMenuId => 0; // No context menu for now
        public override bool SupportsChildren => true;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            try
            {
                var resourceGroups = new List<ResourceGroupNode>();

                await foreach (ResourceGroupResource rg in AzureResourceService.Instance.GetResourceGroupsAsync(SubscriptionId, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    resourceGroups.Add(new ResourceGroupNode(rg.Data.Name, SubscriptionId));
                }

                // Sort alphabetically by name
                foreach (ResourceGroupNode node in resourceGroups.OrderBy(r => r.Label, StringComparer.OrdinalIgnoreCase))
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
