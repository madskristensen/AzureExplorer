using System;
using System.Threading;
using System.Threading.Tasks;

using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Storage.Models
{
    internal enum StorageAccountState
    {
        Unknown,
        Succeeded,
        Failed
    }

    /// <summary>
    /// Represents an Azure Storage Account in the explorer tree. Expandable to show blob containers.
    /// </summary>
    internal sealed class StorageAccountNode : ExplorerNodeBase, IPortalResource
    {
        private StorageAccountState _state;

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
            _state = ParseState(state);

            // Show SKU info as description, or state if failed
            Description = _state == StorageAccountState.Failed
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

        public StorageAccountState State
        {
            get => _state;
            set
            {
                if (SetProperty(ref _state, value))
                {
                    Description = value == StorageAccountState.Failed
                        ? value.ToString()
                        : FormatDescription(Kind, SkuName);
                    OnPropertyChanged(nameof(IconMoniker));
                }
            }
        }

        public override ImageMoniker IconMoniker => State switch
        {
            StorageAccountState.Succeeded => KnownMonikers.AzureStorageAccount,
            StorageAccountState.Failed => KnownMonikers.ApplicationWarning,
            _ => KnownMonikers.AzureStorageAccount
        };

        public override int ContextMenuId => PackageIds.StorageAccountContextMenu;
        public override bool SupportsChildren => true;

        public override async Task LoadChildrenAsync(CancellationToken cancellationToken = default)
        {
            if (!BeginLoading())
                return;

            try
            {
                // Add Blob Containers node
                AddChild(new ContainersNode(SubscriptionId, ResourceGroupName, Label));
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

        internal static StorageAccountState ParseState(string state)
        {
            if (string.IsNullOrEmpty(state))
                return StorageAccountState.Unknown;

            if (state.Equals("Succeeded", StringComparison.OrdinalIgnoreCase))
                return StorageAccountState.Succeeded;

            if (state.Equals("Failed", StringComparison.OrdinalIgnoreCase))
                return StorageAccountState.Failed;

            return StorageAccountState.Unknown;
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
