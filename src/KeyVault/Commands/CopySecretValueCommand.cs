using System.Windows;

using AzureExplorer.Core.Services;
using AzureExplorer.KeyVault.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.KeyVault.Commands
{
    [Command(PackageIds.CopySecretValue)]
    internal sealed class CopySecretValueCommand : BaseCommand<CopySecretValueCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode?.ActualNode is not SecretNode node) return;

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Retrieving secret value...");

                var value = await AzureResourceService.Instance.GetSecretValueAsync(
                    node.SubscriptionId,
                    node.VaultUri,
                    node.Label);

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                Clipboard.SetText(value);

                await VS.StatusBar.ShowMessageAsync($"Secret value copied to clipboard.");
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                await VS.StatusBar.ShowMessageAsync($"Failed to copy secret: {ex.Message}");
            }
        }
    }
}
