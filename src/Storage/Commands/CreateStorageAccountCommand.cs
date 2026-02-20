using AzureExplorer.Core.Services;
using AzureExplorer.Storage.Dialogs;
using AzureExplorer.Storage.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.Storage.Commands
{
    [Command(PackageIds.CreateStorageAccount)]
    internal sealed class CreateStorageAccountCommand : BaseCommand<CreateStorageAccountCommand>
    {
        protected override void BeforeQueryStatus(EventArgs e)
        {
            // Only enable when on a resource group's StorageAccountsNode, not subscription-level
            Command.Enabled = AzureExplorerControl.SelectedNode?.ActualNode is StorageAccountsNode;
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode?.ActualNode is not StorageAccountsNode node)
                return;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dialog = new CreateStorageAccountDialog();

            try
            {
                // Load resource group location and available locations in parallel
                await VS.StatusBar.ShowMessageAsync("Loading Azure locations...");

                var rgLocationTask = AzureResourceService.Instance.GetResourceGroupLocationAsync(
                    node.SubscriptionId, node.ResourceGroupName);
                var locationsTask = AzureResourceService.Instance.GetLocationsAsync(node.SubscriptionId);

                await Task.WhenAll(rgLocationTask, locationsTask);

                var rgLocation = await rgLocationTask;
                var locations = await locationsTask;

                // Pre-select the resource group's location
                dialog.SetLocations(locations, rgLocation);
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
                await VS.StatusBar.ShowMessageAsync($"Creating storage account '{dialog.AccountName}'...");

                await AzureResourceService.Instance.CreateStorageAccountAsync(
                    node.SubscriptionId,
                    node.ResourceGroupName,
                    dialog.AccountName,
                    dialog.SelectedLocation.Name,
                    dialog.SelectedSku.SkuName);

                await VS.StatusBar.ShowMessageAsync($"Storage account '{dialog.AccountName}' created successfully.");

                // Insert the new node, expand parent, and select the new node
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var newNode = node.AddStorageAccount(dialog.AccountName, dialog.SelectedSku.SkuName);
                node.IsExpanded = true;
                AzureExplorerControl.SelectNode(newNode);

                // Notify other views (e.g., subscription-level) that a new storage account was created
                ResourceNotificationService.NotifyCreated(
                    "Microsoft.Storage/storageAccounts",
                    node.SubscriptionId,
                    node.ResourceGroupName,
                    dialog.AccountName,
                    dialog.SelectedSku.SkuName);
            }
            catch (Azure.RequestFailedException ex) when (ex.ErrorCode == "StorageAccountAlreadyTaken")
            {
                await ex.LogAsync();
                await VS.StatusBar.ShowMessageAsync($"Name '{dialog.AccountName}' is already taken.");
                await VS.MessageBox.ShowErrorAsync(
                    "Name Not Available",
                    $"The storage account name '{dialog.AccountName}' is already taken.\n\n" +
                    "Storage account names must be globally unique across all of Azure. " +
                    "Try a more specific name like '{dialog.AccountName}{DateTime.Now:MMdd}' or add your company prefix.");
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                await VS.StatusBar.ShowMessageAsync($"Failed to create storage account: {ex.Message}");
            }
            finally
            {
                // Clear the "Creating..." status
                node.Description = null;
            }
        }
    }
}
