using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.AppService.Models;
using Azure.ResourceManager.Resources;

using AzureExplorer.Core.Services;

namespace AzureExplorer.AppService.Services
{
    /// <summary>
    /// Provides App Service specific operations: start, stop, restart, and state queries.
    /// </summary>
    internal sealed class AppServiceManager
    {
        private static readonly Lazy<AppServiceManager> _instance = new(() => new AppServiceManager());

        private AppServiceManager() { }

        public static AppServiceManager Instance => _instance.Value;

        public async Task StartAsync(string subscriptionId, string resourceGroupName, string name, CancellationToken cancellationToken = default)
        {
            WebSiteResource site = await GetWebSiteAsync(subscriptionId, resourceGroupName, name, cancellationToken);
            await site.StartAsync(cancellationToken);
        }

        public async Task StopAsync(string subscriptionId, string resourceGroupName, string name, CancellationToken cancellationToken = default)
        {
            WebSiteResource site = await GetWebSiteAsync(subscriptionId, resourceGroupName, name, cancellationToken);
            await site.StopAsync(cancellationToken);
        }

        public async Task RestartAsync(string subscriptionId, string resourceGroupName, string name, CancellationToken cancellationToken = default)
        {
            WebSiteResource site = await GetWebSiteAsync(subscriptionId, resourceGroupName, name, cancellationToken);
            await site.RestartAsync(cancellationToken: cancellationToken);
        }

        public async Task<string> GetStateAsync(string subscriptionId, string resourceGroupName, string name, CancellationToken cancellationToken = default)
        {
            WebSiteResource site = await GetWebSiteAsync(subscriptionId, resourceGroupName, name, cancellationToken);
            return site.Data.State;
        }

        public async Task<string> GetDefaultHostNameAsync(string subscriptionId, string resourceGroupName, string name, CancellationToken cancellationToken = default)
        {
            WebSiteResource site = await GetWebSiteAsync(subscriptionId, resourceGroupName, name, cancellationToken);
            return site.Data.DefaultHostName;
        }

        /// <summary>
        /// Enables filesystem-level application and HTTP logging so the Kudu
        /// <c>/api/logstream</c> endpoint has data to stream.
        /// </summary>
        public async Task EnableApplicationLoggingAsync(string subscriptionId, string resourceGroupName, string name, CancellationToken cancellationToken = default)
        {
            WebSiteResource site = await GetWebSiteAsync(subscriptionId, resourceGroupName, name, cancellationToken);

            var data = new SiteLogsConfigData
            {
                ApplicationLogs = new ApplicationLogsConfig
                {
                    FileSystemLevel = WebAppLogLevel.Information
                },
                HttpLogs = new AppServiceHttpLogsConfig
                {
                    FileSystem = new FileSystemHttpLogsConfig
                    {
                        IsEnabled = true,
                        RetentionInMb = 35,
                        RetentionInDays = 1
                    }
                },
                IsDetailedErrorMessagesEnabled = true,
                IsFailedRequestsTracingEnabled = true
            };

            await site.GetLogsSiteConfig().CreateOrUpdateAsync(WaitUntil.Completed, data, cancellationToken);
        }

        /// <summary>
        /// Gets all application settings for the specified App Service.
        /// </summary>
        public async Task<Dictionary<string, string>> GetAppSettingsAsync(string subscriptionId, string resourceGroupName, string name, CancellationToken cancellationToken = default)
        {
            WebSiteResource site = await GetWebSiteAsync(subscriptionId, resourceGroupName, name, cancellationToken);
            AppServiceConfigurationDictionary settings = await site.GetApplicationSettingsAsync(cancellationToken);
            return new Dictionary<string, string>(settings.Properties);
        }

        /// <summary>
        /// Updates all application settings for the specified App Service.
        /// </summary>
        public async Task UpdateAppSettingsAsync(string subscriptionId, string resourceGroupName, string name, IDictionary<string, string> settings, CancellationToken cancellationToken = default)
        {
            WebSiteResource site = await GetWebSiteAsync(subscriptionId, resourceGroupName, name, cancellationToken);
            var configData = new AppServiceConfigurationDictionary();
            foreach (KeyValuePair<string, string> kvp in settings)
            {
                configData.Properties.Add(kvp.Key, kvp.Value);
            }

            await site.UpdateApplicationSettingsAsync(configData, cancellationToken);
        }

        /// <summary>
        /// Gets the publish profile XML for the specified App Service.
        /// </summary>
        public async Task<string> GetPublishProfileAsync(string subscriptionId, string resourceGroupName, string name, CancellationToken cancellationToken = default)
        {
            WebSiteResource site = await GetWebSiteAsync(subscriptionId, resourceGroupName, name, cancellationToken);
            var options = new CsmPublishingProfile
            {
                Format = PublishingProfileFormat.WebDeploy
            };

            Response<Stream> response = await site.GetPublishingProfileXmlWithSecretsAsync(options, cancellationToken);
            using Stream stream = response.Value;
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        private async Task<WebSiteResource> GetWebSiteAsync(string subscriptionId, string resourceGroupName, string name, CancellationToken cancellationToken)
        {
            ArmClient client = AzureResourceService.Instance.GetClient(subscriptionId);
            SubscriptionResource sub = client.GetSubscriptionResource(SubscriptionResource.CreateResourceIdentifier(subscriptionId));
            ResourceGroupResource rg = (await sub.GetResourceGroupAsync(resourceGroupName, cancellationToken)).Value;
            WebSiteCollection webSites = rg.GetWebSites();
            return (await webSites.GetAsync(name, cancellationToken: cancellationToken)).Value;
        }
    }
}
