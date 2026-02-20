using AzureExplorer.AppService.Models;
using AzureExplorer.AppService.Services;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.AppService.Commands
{
    [Command(PackageIds.DeleteFile)]
    internal sealed class DeleteFileCommand : BaseCommand<DeleteFileCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode?.ActualNode is not FileNode node) return;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Confirm deletion
            var confirmed = await VS.MessageBox.ShowConfirmAsync(
                "Delete File",
                $"Are you sure you want to delete '{node.Label}'?\n\nThis action cannot be undone.");

            if (!confirmed) return;

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Deleting '{node.Label}'...");

                await KuduVfsService.Instance.DeleteAsync(
                    node.SubscriptionId,
                    node.AppName,
                    node.RelativePath,
                    isDirectory: false);

                await VS.StatusBar.ShowMessageAsync($"'{node.Label}' deleted.");

                // Remove the deleted file from the tree
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                node.Parent?.Children.Remove(node);
                node.Parent = null;
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                await VS.StatusBar.ShowMessageAsync($"Failed to delete file: {ex.Message}");
            }
        }
    }
}
