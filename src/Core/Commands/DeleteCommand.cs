using AzureExplorer.Core.Models;
using AzureExplorer.Core.Services;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.Core.Commands
{
    /// <summary>
    /// Deletes the selected Azure resource.
    /// Works with any node that implements <see cref="IDeletableResource"/>.
    /// </summary>
    [Command(PackageIds.Delete)]
    internal sealed class DeleteCommand : BaseCommand<DeleteCommand>
    {
        protected override void BeforeQueryStatus(EventArgs e)
        {
            Command.Enabled = AzureExplorerControl.SelectedNode?.ActualNode is IDeletableResource;
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode?.ActualNode is not IDeletableResource resource)
                return;

            // Get the node for UI updates
            var node = AzureExplorerControl.SelectedNode?.ActualNode as ExplorerNodeBase;
            if (node == null)
                return;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Confirm deletion
            bool confirmed = await VS.MessageBox.ShowConfirmAsync(
                $"Delete {resource.DeleteResourceType}",
                $"Are you sure you want to delete '{resource.DeleteResourceName}'?\n\nThis action cannot be undone.");

            if (!confirmed)
                return;

            // Save original description and show "Deleting..." status
            string originalDescription = node.Description;
            node.Description = "Deleting...";

            // Log the activity as in-progress
            var activity = ActivityLogService.Instance.LogActivity(
                "Deleting",
                resource.DeleteResourceName,
                resource.DeleteResourceType);

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Deleting {resource.DeleteResourceType.ToLowerInvariant()} '{resource.DeleteResourceName}'...");

                await resource.DeleteAsync();

                await VS.StatusBar.ShowMessageAsync($"{resource.DeleteResourceType} '{resource.DeleteResourceName}' deleted.");

                // Remove the deleted node from the tree
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (node.Parent is ExplorerNodeBase parent)
                {
                    parent.Children.Remove(node);
                }

                // Mark activity as successful
                activity.Complete();

                // Notify other views that this resource was deleted
                if (!string.IsNullOrEmpty(resource.DeleteResourceProvider))
                {
                    ResourceNotificationService.NotifyDeleted(
                        resource.DeleteResourceProvider,
                        resource.DeleteSubscriptionId,
                        resource.DeleteResourceGroupName,
                        resource.DeleteResourceName);
                }
            }
            catch (Exception ex)
            {
                // Restore original description on failure
                node.Description = originalDescription;

                // Mark activity as failed
                activity.Fail(ex.Message);

                await ex.LogAsync();
                await VS.MessageBox.ShowErrorAsync($"Delete Failed", ex.Message);
            }
        }
    }
}
