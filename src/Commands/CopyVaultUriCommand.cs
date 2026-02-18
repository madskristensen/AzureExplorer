using System.Windows;

using AzureExplorer.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer
{
    [Command(PackageIds.CopyVaultUri)]
    internal sealed class CopyVaultUriCommand : BaseCommand<CopyVaultUriCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode is not KeyVaultNode node) return;

            if (!string.IsNullOrEmpty(node.VaultUri))
            {
                Clipboard.SetText(node.VaultUri);
                await VS.StatusBar.ShowMessageAsync($"Copied: {node.VaultUri}");
            }
        }
    }
}
