using System.Collections.Generic;

using AzureExplorer.AppService.Dialogs;
using AzureExplorer.Core.Models;
using AzureExplorer.AppService.Services;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.AppService.Commands
{
    [Command(PackageIds.ManageAppSettings)]
    internal sealed class ManageAppSettingsCommand : BaseCommand<ManageAppSettingsCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode?.ActualNode is not IWebSiteNode node)
                return;

            try
            {
                await VS.StatusBar.ShowMessageAsync($"Loading app settings for {node.Label}...");

                Dictionary<string, string> settings = await AppServiceManager.Instance.GetAppSettingsAsync(
                    node.SubscriptionId, node.ResourceGroupName, node.Label);

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var dialog = new AppSettingsDialog(node.Label, settings);
                if (dialog.ShowDialog() == true)
                {
                    await VS.StatusBar.ShowMessageAsync($"Saving app settings for {node.Label}...");

                    Dictionary<string, string> updatedSettings = dialog.GetSettingsDictionary();
                    await AppServiceManager.Instance.UpdateAppSettingsAsync(
                        node.SubscriptionId, node.ResourceGroupName, node.Label, updatedSettings);

                    await VS.StatusBar.ShowMessageAsync($"App settings saved for {node.Label}");
                }
                else
                {
                    await VS.StatusBar.ClearAsync();
                }
            }
            catch (Exception ex)
            {
                await VS.MessageBox.ShowErrorAsync("App Settings", $"Failed to manage app settings: {ex.Message}");
            }
        }
    }
}
