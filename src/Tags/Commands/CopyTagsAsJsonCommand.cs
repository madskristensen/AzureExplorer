using System.Collections.Generic;
using System.Text.Json;
using System.Windows;

using AzureExplorer.Core.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.Tags.Commands
{
    /// <summary>
    /// Command to copy tags as JSON to the clipboard.
    /// Works on TagsNode (copies all tags) or on any ITaggableResource node.
    /// </summary>
    [Command(PackageIds.CopyTagsAsJson)]
    internal sealed class CopyTagsAsJsonCommand : BaseCommand<CopyTagsAsJsonCommand>
    {
        protected override Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            ExplorerNodeBase selectedNode = AzureExplorerControl.SelectedNode?.ActualNode;

            IReadOnlyDictionary<string, string> tags = null;

            if (selectedNode is TagsNode tagsNode)
            {
                tags = tagsNode.Tags;
            }
            else if (selectedNode is ITaggableResource taggable)
            {
                tags = taggable.Tags;
            }

            if (tags != null && tags.Count > 0)
            {
                var json = JsonSerializer.Serialize(tags, new JsonSerializerOptions { WriteIndented = true });
                Clipboard.SetText(json);
                VS.StatusBar.ShowMessageAsync($"Copied {tags.Count} tag(s) to clipboard").FireAndForget();
            }

            return Task.CompletedTask;
        }

        protected override void BeforeQueryStatus(EventArgs e)
        {
            ExplorerNodeBase selectedNode = AzureExplorerControl.SelectedNode?.ActualNode;

            var hasTags = false;

            if (selectedNode is TagsNode tagsNode)
            {
                hasTags = tagsNode.Tags.Count > 0;
            }
            else if (selectedNode is ITaggableResource taggable)
            {
                hasTags = taggable.Tags.Count > 0;
            }

            Command.Visible = hasTags;
            Command.Enabled = hasTags;
        }
    }
}
