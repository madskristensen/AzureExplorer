using AzureExplorer.Core.Services;
using AzureExplorer.KeyVault.Dialogs;
using AzureExplorer.KeyVault.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.KeyVault.Commands
{
    [Command(PackageIds.CreateKeyVault)]
    internal sealed class CreateKeyVaultCommand : BaseCommand<CreateKeyVaultCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode?.ActualNode is not KeyVaultsNode node)
                return;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dialog = new CreateKeyVaultDialog();

            try
            {
                // Load available locations
                await VS.StatusBar.ShowMessageAsync("Loading Azure locations...");
                var locations = await AzureResourceService.Instance.GetLocationsAsync(node.SubscriptionId);

                // Try to get the resource group's location as default
                string defaultLocation = await AzureResourceService.Instance.GetResourceGroupLocationAsync(
                    node.SubscriptionId, node.ResourceGroupName);

                dialog.SetLocations(locations, defaultLocation);
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
                await VS.StatusBar.ShowMessageAsync($"Creating Key Vault '{dialog.VaultName}'...");

                await AzureResourceService.Instance.CreateKeyVaultAsync(
                    node.SubscriptionId,
                    node.ResourceGroupName,
                    dialog.VaultName,
                    dialog.SelectedLocation.Name,
                    dialog.SelectedSku.SkuName);

                await VS.StatusBar.ShowMessageAsync($"Key Vault '{dialog.VaultName}' created successfully.");

                // Insert the new node, expand parent, and select the new node
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var newNode = node.AddKeyVault(dialog.VaultName);
                node.IsExpanded = true;
                AzureExplorerControl.SelectNode(newNode);

                // Notify other views (e.g., subscription-level) that a new Key Vault was created
                ResourceNotificationService.NotifyCreated(
                    "Microsoft.KeyVault/vaults",
                    node.SubscriptionId,
                    node.ResourceGroupName,
                    dialog.VaultName);
            }
            catch (Azure.RequestFailedException ex) when (ex.ErrorCode == "VaultAlreadyExists")
            {
                await ex.LogAsync();
                await VS.StatusBar.ShowMessageAsync($"Name '{dialog.VaultName}' is already taken.");
                await VS.MessageBox.ShowErrorAsync(
                    "Name Not Available",
                    $"The Key Vault name '{dialog.VaultName}' is already taken.\n\n" +
                    "Key Vault names must be globally unique across all of Azure.");
            }
            catch (Azure.RequestFailedException ex) when (ex.ErrorCode == "MissingSubscriptionRegistration")
            {
                await ex.LogAsync();
                await VS.StatusBar.ClearAsync();

                // Offer to register the resource provider automatically
                bool shouldRegister = await VS.MessageBox.ShowConfirmAsync(
                    "Resource Provider Not Registered",
                    "The subscription is not registered to use Microsoft.KeyVault.\n\n" +
                    "Would you like to register it now? This may take a minute.");

                if (shouldRegister)
                {
                    try
                    {
                        await VS.StatusBar.ShowMessageAsync("Registering Microsoft.KeyVault resource provider...");

                        await AzureResourceService.Instance.RegisterResourceProviderAsync(
                            node.SubscriptionId,
                            "Microsoft.KeyVault");

                        await VS.StatusBar.ShowMessageAsync("Microsoft.KeyVault registered. Please try creating the Key Vault again.");
                        await VS.MessageBox.ShowAsync(
                            "Registration Started",
                            "The Microsoft.KeyVault resource provider is now registering.\n\n" +
                            "Registration can take a few minutes to complete. Please wait a moment and try creating the Key Vault again.");
                    }
                    catch (Exception regEx)
                    {
                        await regEx.LogAsync();
                        await VS.StatusBar.ShowMessageAsync("Failed to register resource provider.");
                        await VS.MessageBox.ShowErrorAsync(
                            "Registration Failed",
                            $"Failed to register Microsoft.KeyVault: {regEx.Message}\n\n" +
                            "You can manually register using Azure CLI:\n" +
                            "az provider register --namespace Microsoft.KeyVault");
                    }
                }
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                await VS.StatusBar.ShowMessageAsync($"Failed to create Key Vault: {ex.Message}");
            }
            finally
            {
                // Clear the "Creating..." status
                node.Description = null;
            }
        }
    }
}
