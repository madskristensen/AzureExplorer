using AzureExplorer.Core.Services;
using AzureExplorer.KeyVault.Dialogs;
using AzureExplorer.KeyVault.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.KeyVault.Commands
{
    [Command(PackageIds.AddKey)]
    internal sealed class AddKeyCommand : BaseCommand<AddKeyCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            // Support both KeyVaultNode and KeysNode as the selected node
            var selectedNode = AzureExplorerControl.SelectedNode?.ActualNode;

            string subscriptionId;
            string vaultUri;
            KeysNode keysNode = null;

            if (selectedNode is KeyVaultNode kvNode)
            {
                subscriptionId = kvNode.SubscriptionId;
                vaultUri = kvNode.VaultUri;
            }
            else if (selectedNode is KeysNode kNode)
            {
                subscriptionId = kNode.SubscriptionId;
                vaultUri = kNode.VaultUri;
                keysNode = kNode;
            }
            else
            {
                return;
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dialog = new CreateKeyDialog();

            if (dialog.ShowModal() != true)
                return;

            // Show "Creating..." on the keys node if available
            if (keysNode != null)
            {
                keysNode.Description = "Creating...";
            }

            // Log the activity as in-progress
            var activity = ActivityLogService.Instance.LogActivity(
                "Creating",
                dialog.KeyName,
                "Key");

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Creating key '{dialog.KeyName}'...");

                // Determine key parameters based on type
                int? keySizeInBits = dialog.SelectedKeySize?.SizeInBits;
                string curveName = dialog.SelectedKeySize?.CurveName;

                await AzureResourceService.Instance.CreateKeyAsync(
                    subscriptionId,
                    vaultUri,
                    dialog.KeyName,
                    dialog.SelectedKeyType.Value,
                    keySizeInBits,
                    curveName);

                await VS.StatusBar.ShowMessageAsync($"Key '{dialog.KeyName}' created successfully.");

                // Mark activity as successful
                activity.Complete();

                // Add the new key node and select it (if triggered from KeysNode)
                if (keysNode != null)
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var newNode = keysNode.AddKey(dialog.KeyName, dialog.SelectedKeyType.Value);
                    keysNode.IsExpanded = true;
                    AzureExplorerControl.SelectNode(newNode);
                }
                else if (selectedNode is KeyVaultNode keyVaultNode)
                {
                    // Refresh the Key Vault to show the new key
                    await keyVaultNode.RefreshAsync();
                }
            }
            catch (Exception ex)
            {
                // Mark activity as failed
                activity.Fail(ex.Message);

                await ex.LogAsync();
                await VS.StatusBar.ShowMessageAsync($"Failed to create key: {ex.Message}");
            }
            finally
            {
                if (keysNode != null)
                {
                    keysNode.Description = null;
                }
            }
        }
    }
}
