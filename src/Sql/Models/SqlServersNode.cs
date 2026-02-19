using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Sql;

using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Sql.Models
{
    /// <summary>
    /// Category node that groups Azure SQL Servers under a resource group.
    /// </summary>
    internal sealed class SqlServersNode : ExplorerNodeBase
    {
        public SqlServersNode(string subscriptionId, string resourceGroupName)
            : base("SQL Servers")
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }

        public override ImageMoniker IconMoniker => KnownMonikers.AzureSqlDatabase;
        public override int ContextMenuId => PackageIds.SqlServersCategoryContextMenu;
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
                ResourceGroupResource rg = (await sub.GetResourceGroupAsync(ResourceGroupName, cancellationToken)).Value;

                var sqlServers = new List<SqlServerNode>();

                await foreach (SqlServerResource server in rg.GetSqlServers().GetAllAsync(cancellationToken: cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    sqlServers.Add(new SqlServerNode(
                        server.Data.Name,
                        SubscriptionId,
                        ResourceGroupName,
                        server.Data.State?.ToString(),
                        server.Data.FullyQualifiedDomainName,
                        server.Data.Tags));
                }

                // Sort alphabetically by name
                foreach (SqlServerNode node in sqlServers.OrderBy(s => s.Label, StringComparer.OrdinalIgnoreCase))
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
