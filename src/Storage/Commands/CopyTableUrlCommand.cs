using System.Windows;

using AzureExplorer.Storage.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.Storage.Commands
{
    [Command(PackageIds.CopyTableUrl)]
    internal sealed class CopyTableUrlCommand : BaseCommand<CopyTableUrlCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode?.ActualNode is not TableNode node) return;

            if (!string.IsNullOrEmpty(node.TableUrl))
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                Clipboard.SetText(node.TableUrl);
                await VS.StatusBar.ShowMessageAsync($"Copied: {node.TableUrl}");
            }
        }
    }
}
