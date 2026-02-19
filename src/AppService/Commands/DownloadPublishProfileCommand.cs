using System.IO;

using AzureExplorer.Core.Models;
using AzureExplorer.AppService.Services;
using AzureExplorer.ToolWindows;

using Microsoft.Win32;

namespace AzureExplorer.AppService.Commands
{
    [Command(PackageIds.DownloadPublishProfile)]
    internal sealed class DownloadPublishProfileCommand : BaseCommand<DownloadPublishProfileCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode?.ActualNode is not IWebSiteNode node)
            {
                return;
            }

            try
            {
                var profileXml = await AppServiceManager.Instance.GetPublishProfileAsync(node.SubscriptionId, node.ResourceGroupName, node.Label);

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var saveDialog = new SaveFileDialog
                {
                    Title = "Save Publish Profile",
                    FileName = $"{node.Label}.PublishSettings",
                    DefaultExt = ".PublishSettings",
                    Filter = "Publish Settings (*.PublishSettings)|*.PublishSettings|XML Files (*.xml)|*.xml|All Files (*.*)|*.*"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    File.WriteAllText(saveDialog.FileName, profileXml);
                    await VS.StatusBar.ShowMessageAsync($"Publish profile saved to {saveDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                _ = await VS.MessageBox.ShowErrorAsync("Download Publish Profile", $"Failed to download publish profile: {ex.Message}");
            }
        }
    }
}
