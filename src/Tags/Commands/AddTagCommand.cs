using System.Collections.Generic;
using System.Linq;

using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;
using AzureExplorer.Tags.Dialogs;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.Tags.Commands
{
    /// <summary>
    /// Command to add a tag to an Azure resource.
    /// Opens a dialog to enter key and value, then updates the resource via ARM API.
    /// </summary>
    [Command(PackageIds.AddTag)]
    internal sealed class AddTagCommand : BaseCommand<AddTagCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            ExplorerNodeBase selectedNode = AzureExplorerControl.SelectedNode?.ActualNode;

            if (selectedNode is not ITaggableResource taggable)
                return;

            // Get resource info for ARM API call
            string subscriptionId = null;
            string resourceGroup = null;
            string resourceName = null;
            string resourceProvider = null;

            if (selectedNode is IPortalResource portalResource)
            {
                resourceName = portalResource.ResourceName;
                resourceProvider = portalResource.AzureResourceProvider;
            }

            // Extract subscription and resource group from various node types
            switch (selectedNode)
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
                    await VS.MessageBox.ShowWarningAsync("Add Tag", "Cannot add tags to this resource type.");
                    return;
            }

            if (string.IsNullOrEmpty(subscriptionId) || string.IsNullOrEmpty(resourceGroup) ||
                string.IsNullOrEmpty(resourceName) || string.IsNullOrEmpty(resourceProvider))
            {
                await VS.MessageBox.ShowWarningAsync("Add Tag", "Unable to determine resource details.");
                return;
            }

            // Show dialog
            var dialog = new AddTagDialog();
            if (dialog.ShowDialog() != true)
                return;

            var tagKey = dialog.TagKey;
            var tagValue = dialog.TagValue;

            // Build new tags dictionary (existing tags + new tag)
            var newTags = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> kvp in taggable.Tags)
            {
                newTags[kvp.Key] = kvp.Value;
            }
            newTags[tagKey] = tagValue;

            // Log the activity as in-progress
            var activity = ActivityLogService.Instance.LogActivity(
                "Adding Tag",
                $"{tagKey}={tagValue}",
                resourceName);

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Adding tag '{tagKey}' to {resourceName}...");

                await AzureResourceService.Instance.UpdateResourceTagsAsync(
                    subscriptionId,
                    resourceGroup,
                    resourceProvider,
                    resourceName,
                    newTags);

                // Register the new tag with TagService for future suggestions
                TagService.Instance.RegisterTags(new Dictionary<string, string> { { tagKey, tagValue } });

                // Update the UI immediately without collapsing/refreshing
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // Find existing TagsNode or create one
                TagsNode tagsNode = selectedNode.Children.OfType<TagsNode>().FirstOrDefault();

                if (tagsNode == null)
                {
                    // Create new TagsNode and add it at the beginning
                    tagsNode = new TagsNode(newTags);
                    selectedNode.Children.Insert(0, tagsNode);
                    tagsNode.Parent = selectedNode;
                    tagsNode.IsExpanded = true;
                    await tagsNode.LoadChildrenAsync();
                }
                else
                {
                    // Add the new tag to existing TagsNode
                    var newTagNode = new TagNode(tagKey, tagValue)
                    {
                        Parent = tagsNode
                    };
                    tagsNode.Children.Add(newTagNode);
                }

                activity.Complete();
                await VS.StatusBar.ShowMessageAsync($"Tag '{tagKey}' added successfully.");
            }
            catch (Exception ex)
            {
                activity.Fail(ex.Message);
                await VS.MessageBox.ShowErrorAsync("Add Tag", $"Failed to add tag: {ex.Message}");
            }
        }

        protected override void BeforeQueryStatus(EventArgs e)
        {
            ExplorerNodeBase selectedNode = AzureExplorerControl.SelectedNode?.ActualNode;

            // Only show for taggable resources
            var isTaggable = selectedNode is ITaggableResource;

            Command.Visible = isTaggable;
            Command.Enabled = isTaggable;
        }
    }
}
