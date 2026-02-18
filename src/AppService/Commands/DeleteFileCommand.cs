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
            if (AzureExplorerControl.SelectedNode is not FileNode node) return;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Confirm deletion
            bool confirmed = await VS.MessageBox.ShowConfirmAsync(
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

                // Refresh the parent folder to remove the deleted file
                if (node.Parent is FolderNode folder)
                {
                    await folder.RefreshAsync();
                }
                else if (node.Parent is FilesNode files)
                {
                    await files.RefreshAsync();
                }
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                await VS.StatusBar.ShowMessageAsync($"Failed to delete file: {ex.Message}");
            }
        }
    }
}
