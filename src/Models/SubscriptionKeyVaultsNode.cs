using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.Resources;

using AzureExplorer.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Models
{
    /// <summary>
    /// Category node that lists all Azure Key Vaults across the entire subscription.
    /// This allows users to see Key Vaults they have direct access to, even if they
    /// don't have list permissions on the containing resource group.
    /// </summary>
    internal sealed class SubscriptionKeyVaultsNode : ExplorerNodeBase
    {
        public SubscriptionKeyVaultsNode(string subscriptionId)
            : base("Key Vaults")
        {
            SubscriptionId = subscriptionId;
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }

        public override ImageMoniker IconMoniker => KnownMonikers.AzureKeyVault;
        public override int ContextMenuId => PackageIds.KeyVaultsCategoryContextMenu;
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

                int count = 0;

                // Use a timeout since subscription-level queries can be very slow
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                // Iterate by pages to have more control and show progress
                AsyncPageable<KeyVaultResource> vaultsPageable = sub.GetKeyVaultsAsync(top: 100, cancellationToken: linkedCts.Token);

                await foreach (Page<KeyVaultResource> page in vaultsPageable.AsPages().WithCancellation(linkedCts.Token))
                {
                    foreach (KeyVaultResource vault in page.Values)
                    {
                        linkedCts.Token.ThrowIfCancellationRequested();

                        string resourceGroupName = vault.Id.ResourceGroupName;

                        var node = new KeyVaultNode(
                            vault.Data.Name,
                            SubscriptionId,
                            resourceGroupName,
                            vault.Data.Properties.ProvisioningState?.ToString(),
                            vault.Data.Properties.VaultUri?.ToString());

                        InsertChildSorted(node);
                        count++;
                        Description = $"loading... ({count} found)";
                    }
                }
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                await ex.LogAsync();
                Children.Clear();
                Children.Add(new LoadingNode { Label = "Timed out loading Key Vaults" });
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
        private void InsertChildSorted(KeyVaultNode node)
        {
            node.Parent = this;

            int index = 0;
            while (index < Children.Count &&
                   string.Compare(Children[index].Label, node.Label, StringComparison.OrdinalIgnoreCase) < 0)
            {
                index++;
            }

            Children.Insert(index, node);
        }
    }
}
