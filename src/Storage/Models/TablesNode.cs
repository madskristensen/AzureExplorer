using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azure.Data.Tables;
using Azure.Data.Tables.Models;

using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Storage.Models
{
    /// <summary>
    /// Container node representing the "Tables" section under a Storage Account.
    /// Shows storage tables when expanded.
    /// </summary>
    internal sealed class TablesNode : ExplorerNodeBase
    {
        public TablesNode(string subscriptionId, string resourceGroupName, string accountName)
            : base("Tables")
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            AccountName = accountName;

            // Add loading placeholder
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }
        public string AccountName { get; }

        public override ImageMoniker IconMoniker => KnownMonikers.Table;
        public override int ContextMenuId => 0; // No context menu for tables list
        public override bool SupportsChildren => true;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            try
            {
                TableServiceClient client = GetTableServiceClient();
                var tables = new List<TableNode>();

                await foreach (TableItem table in client.QueryAsync(cancellationToken: cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    tables.Add(new TableNode(
                        table.Name,
                        SubscriptionId,
                        ResourceGroupName,
                        AccountName));
                }

                // Sort alphabetically by name
                foreach (TableNode node in tables.OrderBy(t => t.Label, StringComparer.OrdinalIgnoreCase))
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

        private TableServiceClient GetTableServiceClient()
        {
            // Get credential scoped to the subscription's tenant
            Azure.Core.TokenCredential credential = AzureResourceService.Instance.GetCredential(SubscriptionId);

            // Build the table service URI
            var serviceUri = new Uri($"https://{AccountName}.table.core.windows.net");

            return new TableServiceClient(serviceUri, credential);
        }
    }
}
