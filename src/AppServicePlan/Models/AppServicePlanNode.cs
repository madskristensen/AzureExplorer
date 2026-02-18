using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Models
{
    /// <summary>
    /// Represents an Azure App Service Plan. Leaf node with context menu actions.
    /// </summary>
    internal sealed class AppServicePlanNode : ExplorerNodeBase
    {
        public AppServicePlanNode(
            string name,
            string subscriptionId,
            string resourceGroupName,
            string sku,
            string kind,
            int? numberOfSites)
            : base(name)
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            Sku = sku;
            Kind = kind;
            NumberOfSites = numberOfSites ?? 0;
            Description = BuildDescription();
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }
        public string Sku { get; }
        public string Kind { get; }
        public int NumberOfSites { get; }

        public override ImageMoniker IconMoniker => KnownMonikers.CloudService;
        public override int ContextMenuId => PackageIds.AppServicePlanContextMenu;
        public override bool SupportsChildren => false;

        private string BuildDescription()
        {
            var parts = new System.Collections.Generic.List<string>();

            if (!string.IsNullOrEmpty(Sku))
                parts.Add(Sku);

            parts.Add($"{NumberOfSites} site{(NumberOfSites == 1 ? "" : "s")}");

            return string.Join(" | ", parts);
        }
    }
}
