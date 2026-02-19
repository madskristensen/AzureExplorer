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
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            ExplorerNodeBase selectedNode = AzureExplorerControl.SelectedNode?.ActualNode;

            if (selectedNode is TagNode tagNode)
            {
                // Build the search query for the tag
                var searchQuery = string.IsNullOrEmpty(tagNode.Value)
                    ? $"tag:{tagNode.Key}"
                    : $"tag:{tagNode.Key}={tagNode.Value}";

                // Set the search text in the VS search box so the user can see and clear it
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                AzureExplorerWindow.Pane.SetSearchText(searchQuery);
            }
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
