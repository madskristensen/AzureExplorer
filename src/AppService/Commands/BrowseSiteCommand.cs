using System.Diagnostics;

using AzureExplorer.AppService.Services;
using AzureExplorer.Core.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.AppService.Commands
{
    [Command(PackageIds.BrowseSite)]
    internal sealed class BrowseSiteCommand : BaseCommand<BrowseSiteCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode is not IWebSiteNode node)
                return;

            var url = node.BrowseUrl;
            if (string.IsNullOrEmpty(url))
            {
                // Try to fetch the hostname if we don't have it
                try
                {
                    var hostName = await AppServiceManager.Instance.GetDefaultHostNameAsync(
                        node.SubscriptionId, node.ResourceGroupName, node.Label);
                    if (!string.IsNullOrEmpty(hostName))
                        url = $"https://{hostName}";
                }
                catch (Exception ex)
                {
                    await VS.MessageBox.ShowErrorAsync("Browse Site", $"Could not retrieve URL: {ex.Message}");
                    return;
                }
            }

            if (!string.IsNullOrEmpty(url))
            {
                Process.Start(url);
            }
        }
    }
}
