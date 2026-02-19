using System;
using System.Threading.Tasks;
using System.Windows;

using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;

using AzureExplorer.Core.Services;
using AzureExplorer.Storage.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.Storage.Commands
{
    [Command(PackageIds.CopyStorageConnectionString)]
    internal sealed class CopyConnectionStringCommand : BaseCommand<CopyConnectionStringCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (AzureExplorerControl.SelectedNode?.ActualNode is not StorageAccountNode node)
                return;

            try
            {
                await VS.StatusBar.ShowMessageAsync("Retrieving connection string...");

                var connectionString = await GetConnectionStringAsync(
                    node.SubscriptionId,
                    node.ResourceGroupName,
                    node.Label);

                if (!string.IsNullOrEmpty(connectionString))
                {
                    Clipboard.SetText(connectionString);
                    await VS.StatusBar.ShowMessageAsync($"Connection string copied for {node.Label}");
                }
                else
                {
                    await VS.StatusBar.ShowMessageAsync("Failed to retrieve connection string");
                }
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                await VS.StatusBar.ShowMessageAsync($"Error: {ex.Message}");
            }
        }

        private static async Task<string> GetConnectionStringAsync(
            string subscriptionId,
            string resourceGroupName,
            string accountName)
        {
            ArmClient client = AzureResourceService.Instance.GetClient(subscriptionId);

            SubscriptionResource sub = client.GetSubscriptionResource(
                SubscriptionResource.CreateResourceIdentifier(subscriptionId));

            ResourceGroupResource rg = (await sub.GetResourceGroupAsync(resourceGroupName)).Value;

            StorageAccountResource account = (await rg.GetStorageAccountAsync(accountName)).Value;

            // Get the first available key
            string key = null;
            await foreach (StorageAccountKey accountKey in account.GetKeysAsync())
            {
                key = accountKey.Value;
                break;
            }

            if (string.IsNullOrEmpty(key))
                return null;

            // Build connection string
            return $"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={key};EndpointSuffix=core.windows.net";
        }
    }
}
