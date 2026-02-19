using System;
using System.Threading;
using System.Threading.Tasks;

using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Storage.Models
{
    /// <summary>
    /// Represents an Azure Storage Account in the explorer tree. Expandable to show blob containers.
    /// </summary>
    internal sealed class StorageAccountNode : ExplorerNodeBase, IPortalResource
    {
        private ProvisioningState _state;

        public StorageAccountNode(
            string name,
            string subscriptionId,
            string resourceGroupName,
            string state,
            string kind,
            string skuName)
            : base(name)
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
            Kind = kind;
            SkuName = skuName;
            _state = ProvisioningStateParser.Parse(state);

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
