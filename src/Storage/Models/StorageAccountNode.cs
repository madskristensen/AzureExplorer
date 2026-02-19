using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Storage.Models
{
    /// <summary>
    /// Represents an Azure Storage Account in the explorer tree. Expandable to show blob containers.
    /// </summary>
    internal sealed class StorageAccountNode : ExplorerNodeBase, IPortalResource, ITaggableResource
    {
        private ProvisioningState _state;

        public StorageAccountNode(
            string name,
            string subscriptionId,
            string resourceGroupName,
            string state,
            string kind,
            string skuName,
            IDictionary<string, string> tags = null)
            : base(name)
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            Kind = kind;
            SkuName = skuName;
            _state = ProvisioningStateParser.Parse(state);

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

            // Show SKU info as description, or state if failed
            Description = _state == ProvisioningState.Failed
                ? _state.ToString()
                : FormatDescription(kind, skuName);

            // Add loading placeholder for expandable node
            Children.Add(new LoadingNode());
        }

        public string SubscriptionId { get; }
        public string ResourceGroupName { get; }
        public string Kind { get; }
        public string SkuName { get; }

        // IPortalResource
        public string ResourceName => Label;
        public string AzureResourceProvider => "Microsoft.Storage/storageAccounts";

        // ITaggableResource
        public IReadOnlyDictionary<string, string> Tags { get; }
        public string TagsTooltip => Tags.FormatTagsTooltip();
        public bool HasTag(string key, string value = null) => Tags.ContainsTag(key, value);

        public ProvisioningState State
        {
            get => _state;
            set
            {
                if (SetProperty(ref _state, value))
                {
                    Description = value == ProvisioningState.Failed
                        ? value.ToString()
                        : FormatDescription(Kind, SkuName);
                    OnPropertyChanged(nameof(IconMoniker));
                }
            }
        }

        public override ImageMoniker IconMoniker => State switch
        {
            ProvisioningState.Succeeded => KnownMonikers.AzureStorageAccount,
            ProvisioningState.Failed => KnownMonikers.ApplicationWarning,
            _ => KnownMonikers.AzureStorageAccount
        };

        public override int ContextMenuId => PackageIds.StorageAccountContextMenu;
        public override bool SupportsChildren => true;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            await LoadChildrenWithErrorHandlingAsync(_ =>
            {
                // Add Tags node if resource has tags
                if (Tags.Count > 0)
                {
                    AddChild(new TagsNode(Tags));
                }

                // Add Blob Containers node
                AddChild(new ContainersNode(SubscriptionId, ResourceGroupName, Label));
                return Task.CompletedTask;
            }, cancellationToken);
        }

        private static string FormatDescription(string kind, string skuName)
        {
            if (string.IsNullOrEmpty(kind) && string.IsNullOrEmpty(skuName))
                return null;

            // Format: "StorageV2 (Standard_LRS)" or just the available part
            if (!string.IsNullOrEmpty(kind) && !string.IsNullOrEmpty(skuName))
                return $"{kind} ({skuName})";

            return kind ?? skuName;
        }
    }
}
