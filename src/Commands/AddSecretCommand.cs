using AzureExplorer.Dialogs;
using AzureExplorer.Models;
using AzureExplorer.Services;
using AzureExplorer.ToolWindows;

namespace AzureExplorer
{
    [Command(PackageIds.AddSecret)]
    internal sealed class AddSecretCommand : BaseCommand<AddSecretCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode is not KeyVaultNode node) return;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dialog = new AddSecretDialog();

            if (dialog.ShowModal() != true) return;

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Creating secret '{dialog.SecretName}'...");

                await AzureResourceService.Instance.CreateSecretAsync(
                    node.SubscriptionId,
                    node.VaultUri,
                    dialog.SecretName,
                    dialog.SecretValue);

                await VS.StatusBar.ShowMessageAsync($"Secret '{dialog.SecretName}' created successfully.");

                // Refresh the Key Vault to show the new secret
                await node.RefreshAsync();
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                await VS.StatusBar.ShowMessageAsync($"Failed to create secret: {ex.Message}");
            }
        }
    }
}
