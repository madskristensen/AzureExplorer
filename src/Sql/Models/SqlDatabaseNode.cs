using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Sql.Models
{
    internal enum SqlDatabaseStatus
    {
        Unknown,
        Online,
        Offline,
        Creating,
        Paused
    }

    /// <summary>
    /// Represents an Azure SQL Database within a SQL Server.
    /// </summary>
    internal sealed class SqlDatabaseNode : ExplorerNodeBase, IPortalResource
    {
        private SqlDatabaseStatus _status;

        public SqlDatabaseNode(
            string name,
            string subscriptionId,
            string resourceGroupName,
            string serverName,
            string status,
            string edition,
            string serviceLevelObjective)
            : base(name)
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            ServerName = serverName;
            Edition = edition;
            ServiceLevelObjective = serviceLevelObjective;
            _status = ParseStatus(status);

            // Show edition/tier as description, or status if not online
            Description = _status != SqlDatabaseStatus.Online && _status != SqlDatabaseStatus.Unknown
                ? _status.ToString()
                : FormatDescription(edition, serviceLevelObjective);
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }
        public string ServerName { get; }
        public string Edition { get; }
        public string ServiceLevelObjective { get; }

        // IPortalResource - databases are nested under servers
        public string ResourceName => Label;
        public string AzureResourceProvider => "Microsoft.Sql/servers/databases";

        public SqlDatabaseStatus Status
        {
            get => _status;
            set
            {
                if (SetProperty(ref _status, value))
                {
                    Description = value != SqlDatabaseStatus.Online && value != SqlDatabaseStatus.Unknown
                        ? value.ToString()
                        : FormatDescription(Edition, ServiceLevelObjective);
                    OnPropertyChanged(nameof(IconMoniker));
                }
            }
        }

        public override ImageMoniker IconMoniker => Status switch
        {
            SqlDatabaseStatus.Online => KnownMonikers.AzureSqlDatabase,
            SqlDatabaseStatus.Offline => KnownMonikers.DatabaseOffline,
            SqlDatabaseStatus.Paused => KnownMonikers.DatabaseWarning,
            _ => KnownMonikers.AzureSqlDatabase
        };

        public override int ContextMenuId => PackageIds.SqlDatabaseContextMenu;
        public override bool SupportsChildren => false;

        internal static SqlDatabaseStatus ParseStatus(string status)
        {
            if (string.IsNullOrEmpty(status))
                return SqlDatabaseStatus.Unknown;

            if (status.Equals("Online", StringComparison.OrdinalIgnoreCase))
                return SqlDatabaseStatus.Online;

            if (status.Equals("Offline", StringComparison.OrdinalIgnoreCase))
                return SqlDatabaseStatus.Offline;

            if (status.Equals("Creating", StringComparison.OrdinalIgnoreCase))
                return SqlDatabaseStatus.Creating;

            if (status.Equals("Paused", StringComparison.OrdinalIgnoreCase))
                return SqlDatabaseStatus.Paused;

            return SqlDatabaseStatus.Unknown;
        }

        private static string FormatDescription(string edition, string serviceLevelObjective)
        {
            if (string.IsNullOrEmpty(edition) && string.IsNullOrEmpty(serviceLevelObjective))
                return null;

            // Format: "Standard (S0)" or just the available part
            if (!string.IsNullOrEmpty(edition) && !string.IsNullOrEmpty(serviceLevelObjective))
                return $"{edition} ({serviceLevelObjective})";

            return edition ?? serviceLevelObjective;
        }
    }
}
