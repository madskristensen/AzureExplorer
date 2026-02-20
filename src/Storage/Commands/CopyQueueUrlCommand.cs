using System.Windows;

using AzureExplorer.Storage.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.Storage.Commands
{
    [Command(PackageIds.CopyQueueUrl)]
    internal sealed class CopyQueueUrlCommand : BaseCommand<CopyQueueUrlCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode?.ActualNode is not QueueNode node) return;

            if (!string.IsNullOrEmpty(node.QueueUrl))
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                Clipboard.SetText(node.QueueUrl);
                await VS.StatusBar.ShowMessageAsync($"Copied: {node.QueueUrl}");
            }
        }
    }
}
