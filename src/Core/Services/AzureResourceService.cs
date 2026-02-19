using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Sql;

using AzureExplorer.KeyVault.Models;

namespace AzureExplorer.Core.Services
{
    /// <summary>
    /// Provides methods to list Azure subscriptions, resource groups, and resources
    /// using the Azure Resource Manager SDK.
    /// </summary>
    internal sealed class AzureResourceService
    {
        private static readonly Lazy<AzureResourceService> _instance = new(() => new AzureResourceService());

        private readonly ConcurrentDictionary<string, string> _subscriptionTenantMap =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, string> _subscriptionAccountMap =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, ArmClient> _clientCache =
            new(StringComparer.OrdinalIgnoreCase);

        private AzureResourceService() { }

        public static AzureResourceService Instance => _instance.Value;

        /// <summary>
        /// Returns a <see cref="TokenCredential"/> scoped to the correct tenant
        /// for the given subscription. Useful for direct REST calls (e.g. Kudu API).
        /// </summary>
        internal TokenCredential GetCredential(string subscriptionId = null)
        {
            TokenCredential credential;

            // Try to get account-specific credential for this subscription
            if (subscriptionId != null && _subscriptionAccountMap.TryGetValue(subscriptionId, out var accountId))
            {
                credential = AzureAuthService.Instance.GetCredential(accountId);
            }
            else
            {
                // Fall back to first available account
                IReadOnlyList<AccountInfo> accounts = AzureAuthService.Instance.Accounts;
                if (accounts.Count == 0)
                    throw new InvalidOperationException("Not signed in to Azure.");
                credential = accounts[0].Credential;
            }

            if (subscriptionId != null &&
                _subscriptionTenantMap.TryGetValue(subscriptionId, out var tenantId))
            {
                credential = new TenantScopedCredential(credential, tenantId);
            }

            return credential;
        }

        /// <summary>
        /// Gets or creates a cached <see cref="ArmClient"/> scoped to the correct tenant for
        /// the given subscription. When <paramref name="subscriptionId"/> is null
        /// the default (home-tenant) credential is used.
        /// </summary>
        internal ArmClient GetClient(string subscriptionId = null)
        {
            var cacheKey = subscriptionId ?? string.Empty;
            return _clientCache.GetOrAdd(cacheKey, _ => new ArmClient(GetCredential(subscriptionId)));
        }

        /// <summary>
        /// Clears the cached ArmClient instances. Call when credentials change (sign-out/sign-in).
        /// </summary>
        internal void ClearClientCache()
        {
            _clientCache.Clear();
            _subscriptionAccountMap.Clear();
        }

        /// <summary>
        /// Yields all accessible Azure AD tenants for a specific account.
        /// </summary>
        public async IAsyncEnumerable<TenantInfo> GetTenantsAsync(
            string accountId,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            TokenCredential credential = AzureAuthService.Instance.GetCredential(accountId);
            var client = new ArmClient(credential);

            await foreach (TenantResource tenant in client.GetTenants().GetAllAsync(cancellationToken))
            {
                if (tenant.Data.TenantId.HasValue)
                {
                    yield return new TenantInfo(
                        tenant.Data.TenantId.Value.ToString(),
                        tenant.Data.DisplayName ?? tenant.Data.DefaultDomain);
                }
            }
        }

        /// <summary>
        /// Yields all accessible Azure AD tenants for a specific account using silent authentication.
        /// This method will not trigger interactive authentication prompts.
        /// </summary>
        public async IAsyncEnumerable<TenantInfo> GetTenantsForSearchAsync(
            string accountId,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            TokenCredential credential = AzureAuthService.Instance.GetSilentCredential(accountId);
            var client = new ArmClient(credential);

            await foreach (TenantResource tenant in client.GetTenants().GetAllAsync(cancellationToken))
            {
                if (tenant.Data.TenantId.HasValue)
                {
                    yield return new TenantInfo(
                        tenant.Data.TenantId.Value.ToString(),
                        tenant.Data.DisplayName ?? tenant.Data.DefaultDomain);
                }
            }
        }

        /// <summary>
        /// Yields subscriptions belonging to a specific tenant for a specific account.
        /// </summary>
        public async IAsyncEnumerable<SubscriptionResource> GetSubscriptionsForTenantAsync(
            string accountId,
            string tenantId,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            TokenCredential credential = AzureAuthService.Instance.GetCredential(accountId);
            var tenantCredential = new TenantScopedCredential(credential, tenantId);
            var tenantClient = new ArmClient(tenantCredential);

            await foreach (SubscriptionResource sub in tenantClient.GetSubscriptions().GetAllAsync(cancellationToken))
            {
                // Filter to subscriptions owned by this tenant to avoid duplicates from guest access
                if (sub.Data.TenantId.HasValue && sub.Data.TenantId.Value.ToString() != tenantId)
                    continue;

                _subscriptionTenantMap[sub.Data.SubscriptionId] = tenantId;
                _subscriptionAccountMap[sub.Data.SubscriptionId] = accountId;
                yield return sub;
            }
        }

        /// <summary>
        /// Yields subscriptions belonging to a specific tenant using silent authentication.
        /// This method will not trigger interactive authentication prompts.
        /// </summary>
        public async IAsyncEnumerable<SubscriptionResource> GetSubscriptionsForTenantForSearchAsync(
            string accountId,
            string tenantId,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            TokenCredential credential = AzureAuthService.Instance.GetSilentCredential(accountId);
            var tenantCredential = new TenantScopedCredential(credential, tenantId);
            var tenantClient = new ArmClient(tenantCredential);

            await foreach (SubscriptionResource sub in tenantClient.GetSubscriptions().GetAllAsync(cancellationToken))
            {
                // Filter to subscriptions owned by this tenant to avoid duplicates from guest access
                if (sub.Data.TenantId.HasValue && sub.Data.TenantId.Value.ToString() != tenantId)
                    continue;

                _subscriptionTenantMap[sub.Data.SubscriptionId] = tenantId;
                _subscriptionAccountMap[sub.Data.SubscriptionId] = accountId;
                yield return sub;
            }
        }

        /// <summary>
        /// Gets an <see cref="ArmClient"/> with silent authentication for search operations.
        /// This client will not trigger interactive authentication prompts.
        /// </summary>
        internal ArmClient GetSilentClient(string accountId, string tenantId)
        {
            TokenCredential credential = AzureAuthService.Instance.GetSilentCredential(accountId);
            var tenantCredential = new TenantScopedCredential(credential, tenantId);
            return new ArmClient(tenantCredential);
        }

        /// <summary>
        /// Yields resource groups in the given subscription as they are discovered.
        /// </summary>
        public async IAsyncEnumerable<ResourceGroupResource> GetResourceGroupsAsync(
            string subscriptionId,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            ArmClient client = GetClient(subscriptionId);
            SubscriptionResource sub = client.GetSubscriptionResource(SubscriptionResource.CreateResourceIdentifier(subscriptionId));

            await foreach (ResourceGroupResource rg in sub.GetResourceGroups().GetAllAsync(cancellationToken: cancellationToken))
            {
                yield return rg;
            }
        }

        /// <summary>
        /// Lists all resources in the given resource group using the async pageable API.
        /// </summary>
        public async Task<IReadOnlyList<GenericResourceData>> GetResourcesInGroupAsync(string subscriptionId, string resourceGroupName, CancellationToken cancellationToken = default)
        {
            ArmClient client = GetClient(subscriptionId);
            SubscriptionResource sub = client.GetSubscriptionResource(SubscriptionResource.CreateResourceIdentifier(subscriptionId));
            ResourceGroupResource rg = (await sub.GetResourceGroupAsync(resourceGroupName, cancellationToken)).Value;
            var list = new List<GenericResourceData>();

            await foreach (GenericResource resource in rg.GetGenericResourcesAsync(cancellationToken: cancellationToken))
            {
                list.Add(resource.Data);
            }

            return list;
        }

        /// <summary>
        /// Yields secrets in the given Key Vault.
        /// </summary>
        public async IAsyncEnumerable<SecretNode> GetSecretsAsync(
            string subscriptionId,
            string vaultUri,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            TokenCredential credential = GetCredential(subscriptionId);
            var client = new Azure.Security.KeyVault.Secrets.SecretClient(new Uri(vaultUri), credential);

            await foreach (Azure.Security.KeyVault.Secrets.SecretProperties secret in
                client.GetPropertiesOfSecretsAsync(cancellationToken))
            {
                yield return new SecretNode(secret.Name, subscriptionId, vaultUri, secret.Enabled ?? true);
            }
        }

        /// <summary>
        /// Creates a new secret in the given Key Vault.
        /// </summary>
        public async Task CreateSecretAsync(
            string subscriptionId,
            string vaultUri,
            string secretName,
            string secretValue,
            CancellationToken cancellationToken = default)
        {
            TokenCredential credential = GetCredential(subscriptionId);
            var client = new Azure.Security.KeyVault.Secrets.SecretClient(new Uri(vaultUri), credential);

            await client.SetSecretAsync(secretName, secretValue, cancellationToken);
        }

        /// <summary>
        /// Gets the value of a secret from the given Key Vault.
        /// </summary>
        public async Task<string> GetSecretValueAsync(
            string subscriptionId,
            string vaultUri,
            string secretName,
            CancellationToken cancellationToken = default)
        {
            TokenCredential credential = GetCredential(subscriptionId);
            var client = new Azure.Security.KeyVault.Secrets.SecretClient(new Uri(vaultUri), credential);

            Azure.Response<Azure.Security.KeyVault.Secrets.KeyVaultSecret> response =
                await client.GetSecretAsync(secretName, cancellationToken: cancellationToken);

            return response.Value.Value;
        }

        /// <summary>
        /// Deletes a secret from the given Key Vault.
        /// </summary>
        public async Task DeleteSecretAsync(
            string subscriptionId,
            string vaultUri,
            string secretName,
            CancellationToken cancellationToken = default)
        {
            TokenCredential credential = GetCredential(subscriptionId);
            var client = new Azure.Security.KeyVault.Secrets.SecretClient(new Uri(vaultUri), credential);

            await client.StartDeleteSecretAsync(secretName, cancellationToken);
        }

        /// <summary>
        /// Yields databases in the given SQL Server.
        /// </summary>
        public async IAsyncEnumerable<Sql.Models.SqlDatabaseNode> GetSqlDatabasesAsync(
            string subscriptionId,
            string resourceGroupName,
            string serverName,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            ArmClient client = GetClient(subscriptionId);
            SubscriptionResource sub = client.GetSubscriptionResource(SubscriptionResource.CreateResourceIdentifier(subscriptionId));
            ResourceGroupResource rg = (await sub.GetResourceGroupAsync(resourceGroupName, cancellationToken)).Value;
            Azure.ResourceManager.Sql.SqlServerResource server = (await rg.GetSqlServers().GetAsync(serverName, cancellationToken: cancellationToken)).Value;

            await foreach (Azure.ResourceManager.Sql.SqlDatabaseResource db in server.GetSqlDatabases().GetAllAsync(cancellationToken: cancellationToken))
            {
                yield return new Sql.Models.SqlDatabaseNode(
                    db.Data.Name,
                    subscriptionId,
                    resourceGroupName,
                    serverName,
                    db.Data.Status?.ToString(),
                    db.Data.RequestedServiceObjectiveName?.ToString(),
                    db.Data.CurrentServiceObjectiveName);
            }
        }

        /// <summary>
        /// Wraps a <see cref="TokenCredential"/> to inject a specific tenant ID
        /// into every token request, enabling multi-tenant token acquisition.
        /// </summary>
        private sealed class TenantScopedCredential(TokenCredential inner, string tenantId) : TokenCredential
        {
            private static readonly TimeSpan _syncTokenTimeout = TimeSpan.FromSeconds(30);

            public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
            {
                // Add timeout to prevent indefinite blocking when offline
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    cts.CancelAfter(_syncTokenTimeout);

                    var scoped = new TokenRequestContext(
                        requestContext.Scopes,
                        requestContext.ParentRequestId,
                        requestContext.Claims,
                        tenantId);

                    try
                    {
                        return inner.GetToken(scoped, cts.Token);
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                        throw new TimeoutException(
                            $"Token acquisition timed out after {_syncTokenTimeout.TotalSeconds:F0} seconds. " +
                            "Check your network connection and try again.");
                    }
                }
            }

            public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
            {
                var scoped = new TokenRequestContext(
                    requestContext.Scopes,
                    requestContext.ParentRequestId,
                    requestContext.Claims,
                    tenantId);
                return inner.GetTokenAsync(scoped, cancellationToken);
            }
        }

        /// <summary>
        /// Updates the tags on an Azure resource.
        /// </summary>
        /// <param name="subscriptionId">The subscription ID.</param>
        /// <param name="resourceGroup">The resource group name.</param>
        /// <param name="resourceProvider">The resource provider (e.g., "Microsoft.Web/sites").</param>
        /// <param name="resourceName">The resource name.</param>
        /// <param name="tags">The new tags dictionary to apply.</param>
        public async Task UpdateResourceTagsAsync(
            string subscriptionId,
            string resourceGroup,
            string resourceProvider,
            string resourceName,
            IDictionary<string, string> tags,
            CancellationToken cancellationToken = default)
        {
            ArmClient client = GetClient(subscriptionId);

            // Build the resource ID
            var resourceId = ResourceIdentifier.Parse(
                $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/{resourceProvider}/{resourceName}");

            GenericResource resource = client.GetGenericResource(resourceId);

            // Get current resource to preserve other properties
            GenericResource currentResource = await resource.GetAsync(cancellationToken);

            // Create update with new tags
            var data = new GenericResourceData(currentResource.Data.Location)
            {
                Tags = { }
            };

            // Copy new tags
            foreach (KeyValuePair<string, string> tag in tags)
            {
                data.Tags[tag.Key] = tag.Value;
            }

            // Update the resource (this is a PATCH operation that only updates tags)
            await resource.UpdateAsync(Azure.WaitUntil.Completed, data, cancellationToken);
        }
    }

    /// <summary>
    /// Holds basic information about an Azure AD tenant.
    /// </summary>
    internal sealed class TenantInfo(string tenantId, string displayName)
    {
        public string TenantId { get; } = tenantId;
        public string DisplayName { get; } = displayName;
    }
}
