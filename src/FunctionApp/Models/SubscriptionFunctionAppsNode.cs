using System.Collections.Generic;
using System.Text.Json;

using Azure.ResourceManager.Resources;

using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.FunctionApp.Models
{
    /// <summary>
    /// Category node that lists all Function Apps across the entire subscription.
    /// Filters Microsoft.Web/sites by kind containing "functionapp".
    /// </summary>
    internal sealed class SubscriptionFunctionAppsNode(string subscriptionId) : SubscriptionResourceNodeBase("Function Apps", subscriptionId)
    {
        protected override string ResourceType => "Microsoft.Web/sites";

        public override ImageMoniker IconMoniker => KnownMonikers.AzureFunctionsApp;
        public override int ContextMenuId => PackageIds.FunctionAppsCategoryContextMenu;

        protected override bool ShouldIncludeResource(GenericResource resource)
        {
            // Filter to only include Function Apps (kind contains "functionapp")
            return FunctionAppNode.IsFunctionApp(resource.Data.Kind);
        }

        protected override ExplorerNodeBase CreateNodeFromResource(string name, string resourceGroup, GenericResource resource)
        {
            string state = null;
            string defaultHostName = null;

            if (resource.Data.Properties != null)
            {
                try
                {
                    using var doc = JsonDocument.Parse(resource.Data.Properties);
                    JsonElement root = doc.RootElement;

                    if (root.TryGetProperty("state", out JsonElement stateElement))
                    {
                        state = stateElement.GetString();
                    }

                    if (root.TryGetProperty("defaultHostName", out JsonElement hostElement))
                    {
                        defaultHostName = hostElement.GetString();
                    }
                }
                catch
                {
                    // Properties parsing failed; continue with null values
                }
            }

            // Extract tags from resource
            IDictionary<string, string> tags = resource.Data.Tags;

            return new FunctionAppNode(name, SubscriptionId, resourceGroup, state, defaultHostName, tags);
        }
    }
}
