using System.Collections.Generic;
using System.Linq;
using System.Threading;

using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.KeyVault.Models
{
    /// <summary>
    /// Category node that groups Azure Key Vaults under a resource group.
    /// </summary>
    internal sealed class KeyVaultsNode : ExplorerNodeBase
    {
        public KeyVaultsNode(string subscriptionId, string resourceGroupName)
            : base("Key Vaults")
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }

        public override ImageMoniker IconMoniker => KnownMonikers.AzureKeyVault;
        public override int ContextMenuId => PackageIds.KeyVaultsCategoryContextMenu;
        public override bool SupportsChildren => true;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            try
            {
                // Use Azure Resource Graph for fast loading
                IReadOnlyList<ResourceGraphResult> resources = await ResourceGraphService.Instance.QueryByTypeAsync(
                    SubscriptionId,
                    "Microsoft.KeyVault/vaults",
                    ResourceGroupName,
                    cancellationToken);

                foreach (ResourceGraphResult resource in resources.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var state = resource.GetProperty("provisioningState");
                    var vaultUri = resource.GetProperty("vaultUri");

                    AddChild(new KeyVaultNode(
                        resource.Name,
                        SubscriptionId,
                        ResourceGroupName,
                        state,
                        vaultUri,
                        resource.Tags));
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
