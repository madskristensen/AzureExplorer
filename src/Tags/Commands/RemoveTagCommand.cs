using System.Collections.Generic;

using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.Tags.Commands
{
    /// <summary>
    /// Command to remove a tag from an Azure resource.
    /// Confirms with the user, then updates the resource via ARM API.
    /// </summary>
    [Command(PackageIds.RemoveTag)]
    internal sealed class RemoveTagCommand : BaseCommand<RemoveTagCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            ExplorerNodeBase selectedNode = AzureExplorerControl.SelectedNode?.ActualNode;

            if (selectedNode is not TagNode tagNode)
                return;

            // Navigate up: TagNode -> TagsNode -> Resource
            var tagsNode = tagNode.Parent as TagsNode;
            ExplorerNodeBase resourceNode = tagsNode?.Parent;

            if (resourceNode is not ITaggableResource taggable)
            {
                await VS.MessageBox.ShowWarningAsync("Remove Tag", "Cannot determine the parent resource for this tag.");
                return;
            }

            // Get resource info for ARM API call
            string subscriptionId = null;
            string resourceGroup = null;
            string resourceName = null;
            string resourceProvider = null;

            if (resourceNode is IPortalResource portalResource)
            {
                resourceName = portalResource.ResourceName;
                resourceProvider = portalResource.AzureResourceProvider;
            }

            // Extract subscription and resource group from various node types
            switch (resourceNode)
            {
                case AppService.Models.AppServiceNode appService:
                    subscriptionId = appService.SubscriptionId;
                    resourceGroup = appService.ResourceGroupName;
                    break;
                case FunctionApp.Models.FunctionAppNode funcApp:
                    subscriptionId = funcApp.SubscriptionId;
                    resourceGroup = funcApp.ResourceGroupName;
                    break;
                case Storage.Models.StorageAccountNode storage:
                    subscriptionId = storage.SubscriptionId;
                    resourceGroup = storage.ResourceGroupName;
                    break;
                case VirtualMachine.Models.VirtualMachineNode vm:
                    subscriptionId = vm.SubscriptionId;
                    resourceGroup = vm.ResourceGroupName;
                    break;
                case KeyVault.Models.KeyVaultNode kv:
                    subscriptionId = kv.SubscriptionId;
                    resourceGroup = kv.ResourceGroupName;
                    break;
                case Sql.Models.SqlServerNode sql:
                    subscriptionId = sql.SubscriptionId;
                    resourceGroup = sql.ResourceGroupName;
                    break;
                default:
                    await VS.MessageBox.ShowWarningAsync("Remove Tag", "Cannot remove tags from this resource type.");
                    return;
            }

            if (string.IsNullOrEmpty(subscriptionId) || string.IsNullOrEmpty(resourceGroup) ||
                string.IsNullOrEmpty(resourceName) || string.IsNullOrEmpty(resourceProvider))
            {
                await VS.MessageBox.ShowWarningAsync("Remove Tag", "Unable to determine resource details.");
                return;
            }

            // Confirm with user
            var tagDisplay = string.IsNullOrEmpty(tagNode.Value)
                ? tagNode.Key
                : $"{tagNode.Key}={tagNode.Value}";

            if (!await VS.MessageBox.ShowConfirmAsync(
                "Remove Tag",
                $"Are you sure you want to remove the tag '{tagDisplay}' from {resourceName}?"))
            {
                return;
            }

            // Build new tags dictionary without the removed tag
            var newTags = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> kvp in taggable.Tags)
            {
                if (!kvp.Key.Equals(tagNode.Key, StringComparison.OrdinalIgnoreCase))
                {
                    newTags[kvp.Key] = kvp.Value;
                }
            }

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Removing tag '{tagNode.Key}' from {resourceName}...");

                await AzureResourceService.Instance.UpdateResourceTagsAsync(
                    subscriptionId,
                    resourceGroup,
                    resourceProvider,
                    resourceName,
                    newTags);

                // Update the UI immediately - remove the tag node from the tree
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // Remove the TagNode from its parent TagsNode
                tagsNode.Children.Remove(tagNode);

                // Update the TagsNode label to reflect new count
                tagsNode.Label = $"Tags ({tagsNode.Children.Count})";

                // If no tags remain, remove the TagsNode from the resource
                if (tagsNode.Children.Count == 0 && resourceNode != null)
                {
                    resourceNode.Children.Remove(tagsNode);
                }

                await VS.StatusBar.ShowMessageAsync($"Tag '{tagNode.Key}' removed successfully.");
            }
            catch (Exception ex)
            {
                await VS.MessageBox.ShowErrorAsync("Remove Tag", $"Failed to remove tag: {ex.Message}");
            }
        }

        protected override void BeforeQueryStatus(EventArgs e)
        {
            ExplorerNodeBase selectedNode = AzureExplorerControl.SelectedNode?.ActualNode;
            var isTagNode = selectedNode is TagNode;

            Command.Visible = isTagNode;
            Command.Enabled = isTagNode;
        }
    }
}
