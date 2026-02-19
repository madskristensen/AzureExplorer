using System.Windows;

using AzureExplorer.KeyVault.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.KeyVault.Commands
{
    [Command(PackageIds.CopyVaultUri)]
    internal sealed class CopyVaultUriCommand : BaseCommand<CopyVaultUriCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode?.ActualNode is not KeyVaultNode node) return;

            if (!string.IsNullOrEmpty(node.VaultUri))
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                Clipboard.SetText(node.VaultUri);
                await VS.StatusBar.ShowMessageAsync($"Copied: {node.VaultUri}");
            }
        }
    }
}
