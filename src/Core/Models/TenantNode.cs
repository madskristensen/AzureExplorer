using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azure.ResourceManager.Resources;

using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Core.Models
{
    /// <summary>
    /// Represents an Azure AD tenant in the tree view.
    /// Children are <see cref="SubscriptionNode"/> instances belonging to this tenant.
    /// </summary>
    internal sealed class TenantNode : ExplorerNodeBase
    {
        public TenantNode(string tenantId, string displayName, string accountId) : base(displayName ?? tenantId)
        {
            TenantId = tenantId;
            DisplayName = displayName;
            AccountId = accountId;
            Children.Add(new LoadingNode());
        }

        /// <summary>
        /// The Azure AD tenant ID (GUID).
        /// </summary>
        public string TenantId { get; }

        /// <summary>
        /// The display name of the tenant, if available.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// The account ID this tenant belongs to.
        /// </summary>
        public string AccountId { get; }

        public override ImageMoniker IconMoniker => KnownMonikers.AzureActiveDirectory;
        public override int ContextMenuId => PackageIds.TenantContextMenu;
        public override bool SupportsChildren => true;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            try
            {
                var subscriptions = new List<SubscriptionNode>();

                await foreach (SubscriptionResource sub in AzureResourceService.Instance.GetSubscriptionsForTenantAsync(AccountId, TenantId, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    subscriptions.Add(new SubscriptionNode(sub.Data.DisplayName, sub.Data.SubscriptionId));
                }

                // Sort alphabetically by name
                foreach (SubscriptionNode node in subscriptions.OrderBy(s => s.Label, StringComparer.OrdinalIgnoreCase))
                {
                    AddChild(node);
                }
            }
            finally
            {
                EndLoading();
            }
        }
    }
}
