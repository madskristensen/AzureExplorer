using AzureExplorer.Core.Services;
using AzureExplorer.KeyVault.Dialogs;
using AzureExplorer.KeyVault.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.KeyVault.Commands
{
    [Command(PackageIds.AddSecret)]
    internal sealed class AddSecretCommand : BaseCommand<AddSecretCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            // Support both KeyVaultNode and SecretsNode as the selected node
            var selectedNode = AzureExplorerControl.SelectedNode?.ActualNode;

            string subscriptionId;
            string vaultUri;
            SecretsNode secretsNode = null;

            if (selectedNode is KeyVaultNode kvNode)
            {
                subscriptionId = kvNode.SubscriptionId;
                vaultUri = kvNode.VaultUri;
            }
            else if (selectedNode is SecretsNode sNode)
            {
                subscriptionId = sNode.SubscriptionId;
                vaultUri = sNode.VaultUri;
                secretsNode = sNode;
            }
            else
            {
                return;
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dialog = new AddSecretDialog();

            if (dialog.ShowModal() != true) return;

            // Show "Creating..." on the secrets node if available
            if (secretsNode != null)
            {
                secretsNode.Description = "Creating...";
            }

            // Log the activity as in-progress
            var activity = ActivityLogService.Instance.LogActivity(
                "Creating",
                dialog.SecretName,
                "Secret");

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Creating secret '{dialog.SecretName}'...");

                await AzureResourceService.Instance.CreateSecretAsync(
                    subscriptionId,
                    vaultUri,
                    dialog.SecretName,
                    dialog.SecretValue);

                await VS.StatusBar.ShowMessageAsync($"Secret '{dialog.SecretName}' created successfully.");

                // Mark activity as successful
                activity.Complete();

                // Add the new secret node and select it (if triggered from SecretsNode)
                if (secretsNode != null)
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var newNode = secretsNode.AddSecret(dialog.SecretName);
                    secretsNode.IsExpanded = true;
                    AzureExplorerControl.SelectNode(newNode);
                }
                else if (selectedNode is KeyVaultNode keyVaultNode)
                {
                    // Refresh the Key Vault to show the new secret
                    await keyVaultNode.RefreshAsync();
                }
            }
            catch (Exception ex)
            {
                // Mark activity as failed
                activity.Fail(ex.Message);

                await ex.LogAsync();
                await VS.StatusBar.ShowMessageAsync($"Failed to create secret: {ex.Message}");
            }
            finally
            {
                if (secretsNode != null)
                {
                    secretsNode.Description = null;
                }
            }
        }
    }
}
