using AzureExplorer.Core.Models;
using AzureExplorer.Core.Options;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.Core.Commands
{
    /// <summary>
    /// Context menu command to hide or unhide a tenant.
    /// Hidden tenants are filtered from the tree unless ShowAll is enabled.
    /// </summary>
    [Command(PackageIds.HideTenant)]
    internal sealed class HideTenantCommand : BaseCommand<HideTenantCommand>
    {
        protected override void BeforeQueryStatus(EventArgs e)
        {
            Command.Visible = false;

            // Use RightClickedNode instead of SelectedNode to avoid timing issues
            // where SelectedItem hasn't updated yet when BeforeQueryStatus runs
            ExplorerNodeBase selectedNode = AzureExplorerControl.RightClickedNode?.ActualNode;
            if (selectedNode is not TenantNode tenantNode)
            {
                return;
            }

            Command.Visible = true;

            // Change text based on current hidden state
            Command.Text = tenantNode.IsHidden ? "Unhide Tenant" : "Hide Tenant";
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            // Use RightClickedNode for consistency with BeforeQueryStatus
            ExplorerNodeBase selectedNode = AzureExplorerControl.RightClickedNode?.ActualNode;
            if (selectedNode is not TenantNode tenantNode)
            {
                return;
            }

            try
            {
                GeneralOptions options = GeneralOptions.Instance;
                var wasHidden = tenantNode.IsHidden;

                // Toggle the hidden state
                options.ToggleTenantHidden(tenantNode.TenantId);
                await options.SaveAsync();

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (wasHidden)
                {
                    // Tenant was unhidden - update visibility to show it normally
                    tenantNode.NotifyVisibilityChanged();
                    await VS.StatusBar.ShowMessageAsync($"Tenant '{tenantNode.Label}' is now visible.");
                }
                else
                {
                    // Tenant was hidden
                    if (options.ShowAll)
                    {
                        // ShowAll is enabled, just dim it
                        tenantNode.NotifyVisibilityChanged();
                        await VS.StatusBar.ShowMessageAsync($"Tenant '{tenantNode.Label}' is now hidden (visible because Show All is enabled).");
                    }
                    else
                    {
                        // ShowAll is disabled, hide it via visibility binding
                        tenantNode.NotifyVisibilityChanged();
                        await VS.StatusBar.ShowMessageAsync($"Tenant '{tenantNode.Label}' is now hidden.");
                    }
                }
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                await VS.StatusBar.ShowMessageAsync($"Failed to toggle tenant visibility: {ex.Message}");
            }
        }
    }
}
