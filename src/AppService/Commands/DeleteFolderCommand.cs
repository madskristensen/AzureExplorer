using AzureExplorer.AppService.Models;
using AzureExplorer.AppService.Services;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.AppService.Commands
{
    [Command(PackageIds.DeleteFolder)]
    internal sealed class DeleteFolderCommand : BaseCommand<DeleteFolderCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode?.ActualNode is not FolderNode node) return;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Confirm deletion
            var confirmed = await VS.MessageBox.ShowConfirmAsync(
                "Delete Folder",
                $"Are you sure you want to delete the folder '{node.Label}' and all its contents?\n\nThis action cannot be undone.");

            if (!confirmed) return;

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Deleting folder '{node.Label}'...");

                await KuduVfsService.Instance.DeleteAsync(
                    node.SubscriptionId,
                    node.AppName,
                    node.RelativePath,
                    isDirectory: true);

                await VS.StatusBar.ShowMessageAsync($"Folder '{node.Label}' deleted.");

                // Remove the deleted folder from the tree
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                node.Parent?.Children.Remove(node);
                node.Parent = null;
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                await VS.StatusBar.ShowMessageAsync($"Failed to delete folder: {ex.Message}");
            }
        }
    }
}
