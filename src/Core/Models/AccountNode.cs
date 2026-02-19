using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Core.Models
{
    /// <summary>
    /// Root node representing a signed-in Azure account.
    /// Children are <see cref="TenantNode"/> instances representing each accessible tenant.
    /// </summary>
    internal sealed class AccountNode : ExplorerNodeBase
    {
        public AccountNode(string accountId, string accountName) : base(accountName)
        {
            AccountId = accountId;
            // Add placeholder so the expand arrow is shown before children are loaded
            Children.Add(new LoadingNode());
        }

        /// <summary>
        /// The unique identifier for this account (used for sign-out and credential lookup).
        /// </summary>
        public string AccountId { get; }

        public override ImageMoniker IconMoniker => KnownMonikers.Cloud;
        public override int ContextMenuId => PackageIds.AccountContextMenu;
        public override bool SupportsChildren => true;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            try
            {
                var tenants = new List<TenantNode>();

                await foreach (TenantInfo tenant in AzureResourceService.Instance.GetTenantsAsync(AccountId, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Always add all tenants - visibility is controlled by IsVisible property binding
                    tenants.Add(new TenantNode(tenant.TenantId, tenant.DisplayName, AccountId));
                }

                // Sort alphabetically by display name
                foreach (TenantNode node in tenants.OrderBy(t => t.Label, StringComparer.OrdinalIgnoreCase))
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
