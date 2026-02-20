using System.Windows;

using AzureExplorer.KeyVault.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.KeyVault.Commands
{
    [Command(PackageIds.CopyKeyId)]
    internal sealed class CopyKeyIdCommand : BaseCommand<CopyKeyIdCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode?.ActualNode is not KeyNode node) return;

            if (!string.IsNullOrEmpty(node.KeyId))
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                Clipboard.SetText(node.KeyId);
                await VS.StatusBar.ShowMessageAsync($"Copied: {node.KeyId}");
            }
        }
    }
}
