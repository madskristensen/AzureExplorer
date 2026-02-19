using System.Text.Json;

using Azure.ResourceManager.Resources;

using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Sql.Models
{
    /// <summary>
    /// Category node that lists all Azure SQL Servers across the entire subscription.
    /// </summary>
    internal sealed class SubscriptionSqlServersNode : SubscriptionResourceNodeBase
    {
        public SubscriptionSqlServersNode(string subscriptionId)
            : base("SQL Servers", subscriptionId)
        {
        }

        protected override string ResourceType => "Microsoft.Sql/servers";

        public override ImageMoniker IconMoniker => KnownMonikers.AzureSqlDatabase;
        public override int ContextMenuId => PackageIds.SqlServersCategoryContextMenu;

        protected override ExplorerNodeBase CreateNodeFromResource(string name, string resourceGroup, GenericResource resource)
        {
            string state = null;
            string fqdn = null;

            if (resource.Data.Properties != null)
            {
                try
                {
                    using var doc = JsonDocument.Parse(resource.Data.Properties);
                    JsonElement root = doc.RootElement;

                    if (root.TryGetProperty("state", out JsonElement stateElement))
                    {
                        state = stateElement.GetString();
                    }

                    if (root.TryGetProperty("fullyQualifiedDomainName", out JsonElement fqdnElement))
                    {
                        fqdn = fqdnElement.GetString();
                    }
                }
                catch
                {
                    // Properties parsing failed; continue with null values
                }
            }

            return new SqlServerNode(name, SubscriptionId, resourceGroup, state, fqdn);
        }
    }
}
