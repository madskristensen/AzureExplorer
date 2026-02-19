using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Sql.Models
{
    /// <summary>
    /// Category node that lists all Azure SQL Servers across the entire subscription.
    /// </summary>
    internal sealed class SubscriptionSqlServersNode(string subscriptionId) : SubscriptionResourceNodeBase("SQL Servers", subscriptionId)
    {
        protected override string ResourceType => "Microsoft.Sql/servers";

        public override ImageMoniker IconMoniker => KnownMonikers.AzureSqlDatabase;
        public override int ContextMenuId => PackageIds.SqlServersCategoryContextMenu;

        protected override ExplorerNodeBase CreateNodeFromGraphResult(ResourceGraphResult resource)
        {
            var state = resource.GetProperty("state");
            var fqdn = resource.GetProperty("fullyQualifiedDomainName");

            return new SqlServerNode(
                resource.Name,
                SubscriptionId,
                resource.ResourceGroup,
                state,
                fqdn,
                resource.Tags);
        }
    }
}
