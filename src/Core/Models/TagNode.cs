using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using AzureExplorer.Core.Services;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Core.Models
{
    /// <summary>
    /// Represents a single tag key-value pair in the tree.
    /// </summary>
    internal sealed class TagNode : ExplorerNodeBase, IDeletableResource
    {
        public TagNode(string key, string value)
            : base(key)
        {
            Key = key;
            Value = value;
            Description = value;  // Shows "Environment  Production" style in tree
        }

        /// <summary>
        /// The tag key (name).
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// The tag value.
        /// </summary>
        public string Value { get; }

        public override ImageMoniker IconMoniker => KnownMonikers.Bookmark;

        public override int ContextMenuId => PackageIds.TagContextMenu;

        public override bool SupportsChildren => false;

        // IDeletableResource implementation
        string IDeletableResource.DeleteResourceType => "Tag";
        string IDeletableResource.DeleteResourceName => string.IsNullOrEmpty(Value) ? Key : $"{Key}={Value}";
        string IDeletableResource.DeleteResourceProvider => null; // Tags don't appear in multiple views
        string IDeletableResource.DeleteSubscriptionId => null;
        string IDeletableResource.DeleteResourceGroupName => null;

        async Task IDeletableResource.DeleteAsync()
        {
            // Navigate up: TagNode -> TagsNode -> Resource
            if (Parent is not TagsNode tagsNode)
                throw new InvalidOperationException("Cannot determine the parent Tags node.");

            ExplorerNodeBase resourceNode = tagsNode.Parent;

            if (resourceNode is not ITaggableResource taggable)
                throw new InvalidOperationException("Cannot determine the parent resource for this tag.");

            if (resourceNode is not IPortalResource portalResource)
                throw new InvalidOperationException("Cannot determine resource details.");

            // Extract subscription and resource group from the resource node
            string subscriptionId = portalResource.SubscriptionId;
            string resourceGroup = portalResource.ResourceGroupName;
            string resourceName = portalResource.ResourceName;
            string resourceProvider = portalResource.AzureResourceProvider;

            if (string.IsNullOrEmpty(subscriptionId) || string.IsNullOrEmpty(resourceGroup) ||
                string.IsNullOrEmpty(resourceName) || string.IsNullOrEmpty(resourceProvider))
            {
                throw new InvalidOperationException("Unable to determine resource details for tag removal.");
            }

            // Build new tags dictionary without this tag
            var newTags = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> kvp in taggable.Tags)
            {
                if (!kvp.Key.Equals(Key, StringComparison.OrdinalIgnoreCase))
                {
                    newTags[kvp.Key] = kvp.Value;
                }
            }

            await AzureResourceService.Instance.UpdateResourceTagsAsync(
                subscriptionId,
                resourceGroup,
                resourceProvider,
                resourceName,
                newTags);
        }
    }
}
