using AzureExplorer.Core.Services;
using AzureExplorer.KeyVault.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.KeyVault.Commands
{
    [Command(PackageIds.DeleteSecret)]
    internal sealed class DeleteSecretCommand : BaseCommand<DeleteSecretCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode?.ActualNode is not SecretNode node) return;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Confirm deletion
            var confirmed = await VS.MessageBox.ShowConfirmAsync(
                "Delete Secret",
                $"Are you sure you want to delete the secret '{node.Label}'?\n\nThis action cannot be undone.");

            if (!confirmed) return;

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Deleting secret '{node.Label}'...");

                await AzureResourceService.Instance.DeleteSecretAsync(
                    node.SubscriptionId,
                    node.VaultUri,
                    node.Label);

                await VS.StatusBar.ShowMessageAsync($"Secret '{node.Label}' deleted.");

                // Refresh the parent Key Vault to remove the deleted secret
                if (node.Parent is KeyVaultNode keyVault)
                {
                    await keyVault.RefreshAsync();
                }
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                await VS.StatusBar.ShowMessageAsync($"Failed to delete secret: {ex.Message}");
            }
        }
    }
}
