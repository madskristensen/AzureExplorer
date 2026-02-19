using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;

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
    internal sealed class SqlServerNode : ExplorerNodeBase, IPortalResource, ITaggableResource
    {
        private SqlServerState _state;

        public SqlServerNode(
            string name,
            string subscriptionId,
            string resourceGroupName,
            string state,
            string fullyQualifiedDomainName,
            IDictionary<string, string> tags = null)
            : base(name)
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            FullyQualifiedDomainName = fullyQualifiedDomainName;
            _state = ParseState(state);
            Description = _state == SqlServerState.Unavailable ? _state.ToString() : fullyQualifiedDomainName;

            // Store tags, filtering out Azure system/internal tags
            IDictionary<string, string> filteredTags = tags?.FilterUserTags();
            Tags = filteredTags != null && filteredTags.Count > 0
                ? new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(filteredTags))
                : new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

            // Register tags with TagService for filtering
            if (Tags.Count > 0)
            {
                TagService.Instance.RegisterTags(Tags);
            }

            // Add loading placeholder for databases
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }
        public string FullyQualifiedDomainName { get; }

        // IPortalResource
        public string ResourceName => Label;
        public string AzureResourceProvider => "Microsoft.Sql/servers";

        // ITaggableResource
        public IReadOnlyDictionary<string, string> Tags { get; }
        public string TagsTooltip => Tags.FormatTagsTooltip();
        public bool HasTag(string key, string value = null) => Tags.ContainsTag(key, value);

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

            await LoadChildrenWithErrorHandlingAsync(async ct =>
            {
                // Add Tags node if resource has tags
                if (Tags.Count > 0)
                {
                    AddChild(new TagsNode(Tags));
                }

                await foreach (SqlDatabaseNode db in AzureResourceService.Instance
                    .GetSqlDatabasesAsync(SubscriptionId, ResourceGroupName, Label, ct))
                {
                    ct.ThrowIfCancellationRequested();
                    AddChild(db);
                }
            }, cancellationToken);
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
