using System.Threading;

using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Sql.Models
{
    internal enum SqlServerState
    {
        Unknown,
        Ready,
        Creating,
        Unavailable
    }

    /// <summary>
    /// Represents an Azure SQL Server. Expandable node containing databases.
    /// </summary>
    internal sealed class SqlServerNode : ExplorerNodeBase, IPortalResource
    {
        private SqlServerState _state;

        public SqlServerNode(
            string name,
            string subscriptionId,
            string resourceGroupName,
            string state,
            string fullyQualifiedDomainName)
            : base(name)
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            FullyQualifiedDomainName = fullyQualifiedDomainName;
            _state = ParseState(state);
            Description = _state == SqlServerState.Unavailable ? _state.ToString() : fullyQualifiedDomainName;

            // Add loading placeholder for databases
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }
        public string FullyQualifiedDomainName { get; }

        // IPortalResource
        public string ResourceName => Label;
        public string AzureResourceProvider => "Microsoft.Sql/servers";

        public SqlServerState State
        {
            get => _state;
            set
            {
                if (SetProperty(ref _state, value))
                {
                    Description = value == SqlServerState.Unavailable
                        ? value.ToString()
                        : FullyQualifiedDomainName;
                    OnPropertyChanged(nameof(IconMoniker));
                }
            }
        }

        public override ImageMoniker IconMoniker => State switch
        {
            SqlServerState.Ready => KnownMonikers.AzureSqlDatabase,
            SqlServerState.Unavailable => KnownMonikers.ApplicationWarning,
            _ => KnownMonikers.AzureSqlDatabase
        };

        public override int ContextMenuId => PackageIds.SqlServerContextMenu;
        public override bool SupportsChildren => true;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            try
            {
                await foreach (SqlDatabaseNode db in AzureExplorer.Core.Services.AzureResourceService.Instance
                    .GetSqlDatabasesAsync(SubscriptionId, ResourceGroupName, Label, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    AddChild(db);
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

        internal static SqlServerState ParseState(string state)
        {
            if (string.IsNullOrEmpty(state))
                return SqlServerState.Unknown;

            if (state.Equals("Ready", StringComparison.OrdinalIgnoreCase))
                return SqlServerState.Ready;

            if (state.Equals("Creating", StringComparison.OrdinalIgnoreCase))
                return SqlServerState.Creating;

            if (state.Equals("Unavailable", StringComparison.OrdinalIgnoreCase))
                return SqlServerState.Unavailable;

            return SqlServerState.Unknown;
        }
    }
}
