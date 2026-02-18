using AzureExplorer.Core.Services;
using AzureExplorer.KeyVault.Dialogs;
using AzureExplorer.KeyVault.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.KeyVault.Commands
{
    [Command(PackageIds.UpdateSecretValue)]
    internal sealed class UpdateSecretCommand : BaseCommand<UpdateSecretCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode is not SecretNode node) return;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dialog = new UpdateSecretDialog(node.Label);

            if (dialog.ShowModal() != true) return;

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Updating secret '{node.Label}'...");

                await AzureResourceService.Instance.CreateSecretAsync(
                    node.SubscriptionId,
                    node.VaultUri,
                    node.Label,
                    dialog.SecretValue);

                await VS.StatusBar.ShowMessageAsync($"Secret '{node.Label}' updated successfully.");
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                await VS.StatusBar.ShowMessageAsync($"Failed to update secret: {ex.Message}");
            }
        }
    }
}
