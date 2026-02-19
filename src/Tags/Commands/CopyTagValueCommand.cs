using System.Windows;

using AzureExplorer.Core.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.Tags.Commands
{
    /// <summary>
    /// Command to copy a single tag value to the clipboard.
    /// Works on TagNode.
    /// </summary>
    [Command(PackageIds.CopyTagValue)]
    internal sealed class CopyTagValueCommand : BaseCommand<CopyTagValueCommand>
    {
        protected override Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            ExplorerNodeBase selectedNode = AzureExplorerControl.SelectedNode?.ActualNode;

            if (selectedNode is TagNode tagNode)
            {
                Clipboard.SetText(tagNode.Value);
                VS.StatusBar.ShowMessageAsync($"Copied tag value to clipboard").FireAndForget();
            }

            return Task.CompletedTask;
        }

        protected override void BeforeQueryStatus(EventArgs e)
        {
            ExplorerNodeBase selectedNode = AzureExplorerControl.SelectedNode?.ActualNode;
            var isTagNode = selectedNode is TagNode;

            Command.Visible = isTagNode;
            Command.Enabled = isTagNode;
        }
    }
}
