using System.Windows;

using AzureExplorer.Models;
using AzureExplorer.Services;
using AzureExplorer.ToolWindows;

namespace AzureExplorer
{
    [Command(PackageIds.CopySecretValue)]
    internal sealed class CopySecretValueCommand : BaseCommand<CopySecretValueCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode is not SecretNode node) return;

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Retrieving secret value...");

                string value = await AzureResourceService.Instance.GetSecretValueAsync(
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
