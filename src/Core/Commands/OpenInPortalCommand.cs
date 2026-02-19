using System.Diagnostics;

using AzureExplorer.Core.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.Core.Commands
{
    /// <summary>
    /// Opens the selected Azure resource in the Azure Portal.
    /// Works with any node that implements <see cref="IPortalResource"/>.
    /// </summary>
    [Command(PackageIds.OpenInPortal)]
    internal sealed class OpenInPortalCommand : BaseCommand<OpenInPortalCommand>
    {
        protected override void BeforeQueryStatus(EventArgs e)
        {
            Command.Enabled = AzureExplorerControl.SelectedNode?.ActualNode is IPortalResource;
        }

        protected override Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode?.ActualNode is IPortalResource resource)
            {
                Process.Start(resource.GetPortalUrl());
            }

            return Task.CompletedTask;
        }
    }
}
