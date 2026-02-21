using System.Collections.Generic;
using System.ComponentModel;

namespace AzureExplorer.Core.Options
{
    /// <summary>
    /// General options for the Azure Explorer extension.
    /// </summary>
    internal sealed class GeneralOptions : BaseOptionModel<GeneralOptions>
    {
        /// <summary>
        /// Gets or sets whether to show all nodes including hidden tenants, subscriptions, and empty resource types.
        /// When true, all nodes are visible (hidden items shown dimmed).
        /// When false (default), hidden items and empty resource types are filtered out.
        /// </summary>
        [Category("Display")]
        [DisplayName("Show All")]
        [Description("When enabled, shows all nodes including hidden tenants/subscriptions (dimmed) and empty resource types.")]
        [DefaultValue(false)]
        public bool ShowAll { get; set; }

        /// <summary>
        /// Gets or sets the collection of tenant IDs that are hidden from the tree view.
        /// These tenants are only visible when <see cref="ShowAll"/> is enabled.
        /// </summary>
        [Browsable(false)]
        public HashSet<string> HiddenTenants { get; set; } = [];

        /// <summary>
        /// Gets or sets the collection of subscription IDs that are hidden from the tree view.
        /// These subscriptions are only visible when <see cref="ShowAll"/> is enabled.
        /// </summary>
        [Browsable(false)]
        public HashSet<string> HiddenSubscriptions { get; set; } = [];

        /// <summary>
        /// Checks if a tenant is hidden.
        /// </summary>
        public bool IsTenantHidden(string tenantId)
        {
            return HiddenTenants?.Contains(tenantId) == true;
        }

        /// <summary>
        /// Toggles the hidden state of a tenant.
        /// </summary>
        public void ToggleTenantHidden(string tenantId)
        {
            HiddenTenants ??= [];

            if (HiddenTenants.Contains(tenantId))
            {
                HiddenTenants.Remove(tenantId);
            }
            else
            {
                HiddenTenants.Add(tenantId);
            }
        }

        /// <summary>
        /// Checks if a subscription is hidden.
        /// </summary>
        public bool IsSubscriptionHidden(string subscriptionId)
        {
            return HiddenSubscriptions?.Contains(subscriptionId) == true;
        }

        /// <summary>
        /// Toggles the hidden state of a subscription.
        /// </summary>
        public void ToggleSubscriptionHidden(string subscriptionId)
        {
            HiddenSubscriptions ??= [];

            if (HiddenSubscriptions.Contains(subscriptionId))
            {
                HiddenSubscriptions.Remove(subscriptionId);
            }
            else
            {
                HiddenSubscriptions.Add(subscriptionId);
            }
        }

        /// <summary>
        /// Gets or sets whether the Activity Log panel is visible.
        /// </summary>
        [Category("Activity Log")]
        [DisplayName("Show Activity Log")]
        [Description("When enabled, shows the Activity Log panel at the bottom of the Azure Explorer.")]
        [DefaultValue(true)]
        public bool ShowActivityLog { get; set; } = true;

        /// <summary>
        /// Gets or sets the height of the Activity Log panel in pixels.
        /// </summary>
        [Browsable(false)]
        [DefaultValue(120.0)]
        public double ActivityLogPanelHeight { get; set; } = 120.0;

        /// <summary>
        /// Gets or sets the paths of nodes that are expanded in the tree view.
        /// Stored as a list for JSON serialization compatibility.
        /// </summary>
        [Browsable(false)]
        public List<string> ExpandedNodes { get; set; } = [];
    }
}
