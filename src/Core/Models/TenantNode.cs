using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azure.ResourceManager.Resources;

using AzureExplorer.Core.Options;
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

        /// <summary>
        /// Gets whether this tenant is marked as hidden in settings.
        /// Hidden tenants are only visible when ShowAll is enabled.
        /// </summary>
        public bool IsHidden => GeneralOptions.Instance.IsTenantHidden(TenantId);

        /// <summary>
        /// Gets whether this node should be visible in the tree view.
        /// Hidden tenants are only visible when ShowAll is enabled.
        /// </summary>
        public override bool IsVisible => !IsHidden || GeneralOptions.Instance.ShowAll;

        /// <summary>
        /// Gets the opacity for this node. Returns 0.5 (dimmed) if hidden, otherwise 1.0.
        /// </summary>
        public override double Opacity => IsHidden ? 0.5 : 1.0;

        /// <summary>
        /// Notifies the UI that the visibility and opacity properties have changed.
        /// Call this after toggling the hidden state or ShowAll setting.
        /// </summary>
        public void NotifyVisibilityChanged()
        {
            OnPropertyChanged(nameof(IsVisible));
            OnPropertyChanged(nameof(Opacity));
        }

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

                    // Always add all subscriptions - visibility is controlled by IsVisible property binding
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
