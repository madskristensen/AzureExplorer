using AzureExplorer.Core.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.Tags.Commands
{
    /// <summary>
    /// Command to search for resources with the selected tag.
    /// Performs a tag search directly in the tree view.
    /// </summary>
    [Command(PackageIds.FilterByTag)]
    internal sealed class FilterByTagCommand : BaseCommand<FilterByTagCommand>
    {
        protected override Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            ExplorerNodeBase selectedNode = AzureExplorerControl.SelectedNode?.ActualNode;

            if (selectedNode is TagNode tagNode)
            {
                // Perform tag search directly
                AzureExplorerControl.PerformTagSearch(tagNode.Key, tagNode.Value);
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
