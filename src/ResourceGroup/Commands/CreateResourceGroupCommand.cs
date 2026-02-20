using AzureExplorer.Core.Services;
using AzureExplorer.ResourceGroup.Dialogs;
using AzureExplorer.ResourceGroup.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.ResourceGroup.Commands
{
    [Command(PackageIds.CreateResourceGroup)]
    internal sealed class CreateResourceGroupCommand : BaseCommand<CreateResourceGroupCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode?.ActualNode is not ResourceGroupsNode node)
                return;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dialog = new CreateResourceGroupDialog();

            try
            {
                // Load available locations
                await VS.StatusBar.ShowMessageAsync("Loading Azure locations...");
                var locations = await AzureResourceService.Instance.GetLocationsAsync(node.SubscriptionId);
                dialog.SetLocations(locations);
                await VS.StatusBar.ClearAsync();
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                await VS.StatusBar.ShowMessageAsync($"Failed to load locations: {ex.Message}");
                return;
            }

            if (dialog.ShowModal() != true)
                return;

            // Show "Creating..." on the parent node while API call is in progress
            node.Description = "Creating...";

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Creating resource group '{dialog.ResourceGroupName}'...");

                await AzureResourceService.Instance.CreateResourceGroupAsync(
                    node.SubscriptionId,
                    dialog.ResourceGroupName,
                    dialog.SelectedLocation.Name);

                await VS.StatusBar.ShowMessageAsync($"Resource group '{dialog.ResourceGroupName}' created successfully.");

                // Insert the new node, expand parent, and select the new node
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var newNode = node.AddResourceGroup(dialog.ResourceGroupName);
                node.IsExpanded = true;
                AzureExplorerControl.SelectNode(newNode);

                // Notify other views (for consistency with other resource types)
                ResourceNotificationService.NotifyCreated(
                    "Microsoft.Resources/resourceGroups",
                    node.SubscriptionId,
                    dialog.ResourceGroupName, // Resource group name is its own "resource group"
                    dialog.ResourceGroupName);
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                await VS.StatusBar.ShowMessageAsync($"Failed to create resource group: {ex.Message}");
            }
            finally
            {
                // Clear the "Creating..." status
                node.Description = null;
            }
        }
    }
}
