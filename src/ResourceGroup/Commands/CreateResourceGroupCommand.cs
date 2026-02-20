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

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Creating resource group '{dialog.ResourceGroupName}'...");

                await AzureResourceService.Instance.CreateResourceGroupAsync(
                    node.SubscriptionId,
                    dialog.ResourceGroupName,
                    dialog.SelectedLocation.Name);

                await VS.StatusBar.ShowMessageAsync($"Resource group '{dialog.ResourceGroupName}' created successfully.");

                // Insert the new node directly in sorted order (no refresh needed)
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                node.AddResourceGroup(dialog.ResourceGroupName);
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                await VS.StatusBar.ShowMessageAsync($"Failed to create resource group: {ex.Message}");
            }
        }
    }
}
