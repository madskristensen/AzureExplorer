using System.Windows;

using AzureExplorer.KeyVault.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.KeyVault.Commands
{
    [Command(PackageIds.CopyCertificateId)]
    internal sealed class CopyCertificateIdCommand : BaseCommand<CopyCertificateIdCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode?.ActualNode is not CertificateNode node) return;

            if (!string.IsNullOrEmpty(node.CertificateId))
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                Clipboard.SetText(node.CertificateId);
                await VS.StatusBar.ShowMessageAsync($"Copied: {node.CertificateId}");
            }
        }
    }
}
