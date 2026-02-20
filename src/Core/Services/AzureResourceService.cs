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
        /// Creates an <see cref="ArmClient"/> using a constructor overload that is more stable
        /// across different versions of Azure.ResourceManager that may be loaded by other VS extensions.
        /// </summary>
        private static ArmClient CreateArmClient(TokenCredential credential)
        {
            // Use the constructor with ArmClientOptions to avoid version conflicts.
            // The simple ArmClient(TokenCredential) constructor may not exist in all versions
            // of Azure.ResourceManager that could be loaded by other VS extensions/workloads.
            return new ArmClient(credential, default, new ArmClientOptions());
        }

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
            return _clientCache.GetOrAdd(cacheKey, _ => CreateArmClient(GetCredential(subscriptionId)));
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
        /// Gets the account ID and tenant ID for a subscription (if known).
        /// Returns null if the subscription hasn't been loaded yet.
        /// </summary>
        internal (string AccountId, string TenantId)? GetSubscriptionContext(string subscriptionId)
        {
            if (_subscriptionAccountMap.TryGetValue(subscriptionId, out var accountId) &&
                _subscriptionTenantMap.TryGetValue(subscriptionId, out var tenantId))
            {
                return (accountId, tenantId);
            }
            return null;
        }

        /// <summary>
        /// Gets an ArmClient with the correct tenant context for Resource Graph queries.
        /// </summary>
        internal ArmClient GetClientForResourceGraph(string subscriptionId)
        {
            (string AccountId, string TenantId)? context = GetSubscriptionContext(subscriptionId);
            if (context.HasValue)
            {
                return GetSilentClient(context.Value.AccountId, context.Value.TenantId);
            }
            // Fall back to the regular client if we don't have context
            return GetClient(subscriptionId);
        }

        /// <summary>
        /// Yields all accessible Azure AD tenants for a specific account.
        /// </summary>
        public async IAsyncEnumerable<TenantInfo> GetTenantsAsync(
            string accountId,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            TokenCredential credential = AzureAuthService.Instance.GetCredential(accountId);
            ArmClient client = CreateArmClient(credential);

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
            ArmClient client = CreateArmClient(credential);

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
            ArmClient tenantClient = CreateArmClient(tenantCredential);

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
            ArmClient tenantClient = CreateArmClient(tenantCredential);

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
            return CreateArmClient(tenantCredential);
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
        /// Gets the location of a resource group.
        /// </summary>
        public async Task<string> GetResourceGroupLocationAsync(
            string subscriptionId,
            string resourceGroupName,
            CancellationToken cancellationToken = default)
        {
            ArmClient client = GetClient(subscriptionId);
            SubscriptionResource sub = client.GetSubscriptionResource(SubscriptionResource.CreateResourceIdentifier(subscriptionId));
            ResourceGroupResource rg = (await sub.GetResourceGroupAsync(resourceGroupName, cancellationToken)).Value;

            return rg.Data.Location.Name;
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
        /// Creates a new cryptographic key in the given Key Vault.
        /// </summary>
        /// <param name="subscriptionId">The subscription ID.</param>
        /// <param name="vaultUri">The Key Vault URI.</param>
        /// <param name="keyName">The name of the key to create.</param>
        /// <param name="keyType">The key type (RSA or EC).</param>
        /// <param name="keySizeInBits">The key size in bits (for RSA keys).</param>
        /// <param name="curveName">The curve name (for EC keys).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task CreateKeyAsync(
            string subscriptionId,
            string vaultUri,
            string keyName,
            string keyType,
            int? keySizeInBits = null,
            string curveName = null,
            CancellationToken cancellationToken = default)
        {
            TokenCredential credential = GetCredential(subscriptionId);
            var client = new Azure.Security.KeyVault.Keys.KeyClient(new Uri(vaultUri), credential);

            if (keyType == "RSA")
            {
                var options = new Azure.Security.KeyVault.Keys.CreateRsaKeyOptions(keyName)
                {
                    KeySize = keySizeInBits ?? 2048
                };
                await client.CreateRsaKeyAsync(options, cancellationToken);
            }
            else if (keyType == "EC")
            {
                var options = new Azure.Security.KeyVault.Keys.CreateEcKeyOptions(keyName);
                if (!string.IsNullOrEmpty(curveName))
                {
                    options.CurveName = new Azure.Security.KeyVault.Keys.KeyCurveName(curveName);
                }
                await client.CreateEcKeyAsync(options, cancellationToken);
            }
            else
            {
                throw new System.ArgumentException($"Unsupported key type: {keyType}", nameof(keyType));
            }
        }

        /// <summary>
        /// Yields keys in the given Key Vault.
        /// </summary>
        public async IAsyncEnumerable<KeyVault.Models.KeyNode> GetKeysAsync(
            string subscriptionId,
            string vaultUri,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            TokenCredential credential = GetCredential(subscriptionId);
            var client = new Azure.Security.KeyVault.Keys.KeyClient(new Uri(vaultUri), credential);

            await foreach (Azure.Security.KeyVault.Keys.KeyProperties key in
                client.GetPropertiesOfKeysAsync(cancellationToken))
            {
                yield return new KeyVault.Models.KeyNode(
                    key.Name,
                    subscriptionId,
                    vaultUri,
                    key.Enabled ?? true,
                    null); // KeyType is retrieved only when getting the full key
            }
        }

        /// <summary>
        /// Yields certificates in the given Key Vault.
        /// </summary>
        public async IAsyncEnumerable<KeyVault.Models.CertificateNode> GetCertificatesAsync(
            string subscriptionId,
            string vaultUri,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            TokenCredential credential = GetCredential(subscriptionId);
            var client = new Azure.Security.KeyVault.Certificates.CertificateClient(new Uri(vaultUri), credential);

            await foreach (Azure.Security.KeyVault.Certificates.CertificateProperties cert in
                client.GetPropertiesOfCertificatesAsync(includePending: false, cancellationToken))
            {
                yield return new KeyVault.Models.CertificateNode(
                    cert.Name,
                    subscriptionId,
                    vaultUri,
                    cert.Enabled ?? true,
                    cert.ExpiresOn);
            }
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
        /// Gets available Azure locations for a subscription.
        /// </summary>
        public async Task<IReadOnlyList<AzureLocation>> GetLocationsAsync(
            string subscriptionId,
            CancellationToken cancellationToken = default)
        {
            ArmClient client = GetClient(subscriptionId);
            SubscriptionResource sub = client.GetSubscriptionResource(SubscriptionResource.CreateResourceIdentifier(subscriptionId));

            var locations = new List<AzureLocation>();

            // GetLocationsAsync returns AsyncPageable<LocationExpanded>
            await foreach (Azure.ResourceManager.Resources.Models.LocationExpanded location in sub.GetLocationsAsync(includeExtendedLocations: false, cancellationToken: cancellationToken))
            {
                // Only include physical regions, not logical/extended locations
                if (!string.IsNullOrEmpty(location.Name) && !string.IsNullOrEmpty(location.DisplayName))
                {
                    locations.Add(new AzureLocation(location.Name, location.DisplayName));
                }
            }

            // Sort by display name
            locations.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.DisplayName, b.DisplayName));
            return locations;
        }

        /// <summary>
        /// Creates a new resource group in the given subscription.
        /// </summary>
        public async Task<ResourceGroupResource> CreateResourceGroupAsync(
            string subscriptionId,
            string resourceGroupName,
            string location,
            CancellationToken cancellationToken = default)
        {
            ArmClient client = GetClient(subscriptionId);
            SubscriptionResource sub = client.GetSubscriptionResource(SubscriptionResource.CreateResourceIdentifier(subscriptionId));

            var data = new ResourceGroupData(new Azure.Core.AzureLocation(location));
            ArmOperation<ResourceGroupResource> operation = await sub.GetResourceGroups()
                .CreateOrUpdateAsync(Azure.WaitUntil.Completed, resourceGroupName, data, cancellationToken);

            return operation.Value;
        }

        /// <summary>
        /// Checks if a resource group is empty (contains no resources).
        /// </summary>
        public async Task<bool> IsResourceGroupEmptyAsync(
            string subscriptionId,
            string resourceGroupName,
            CancellationToken cancellationToken = default)
        {
            ArmClient client = GetClient(subscriptionId);
            SubscriptionResource sub = client.GetSubscriptionResource(SubscriptionResource.CreateResourceIdentifier(subscriptionId));
            ResourceGroupResource rg = (await sub.GetResourceGroupAsync(resourceGroupName, cancellationToken)).Value;

            // Check if there are any resources in the group
            await foreach (GenericResource _ in rg.GetGenericResourcesAsync(cancellationToken: cancellationToken))
            {
                // Found at least one resource, so it's not empty
                return false;
            }

            return true;
        }

        /// <summary>
        /// Registers a resource provider for a subscription.
        /// </summary>
        /// <param name="subscriptionId">The subscription ID.</param>
        /// <param name="resourceProviderNamespace">The resource provider namespace (e.g., "Microsoft.KeyVault").</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task RegisterResourceProviderAsync(
            string subscriptionId,
            string resourceProviderNamespace,
            CancellationToken cancellationToken = default)
        {
            ArmClient client = GetClient(subscriptionId);
            SubscriptionResource sub = client.GetSubscriptionResource(SubscriptionResource.CreateResourceIdentifier(subscriptionId));

            ResourceProviderResource provider = await sub.GetResourceProviderAsync(resourceProviderNamespace, cancellationToken: cancellationToken);
            await provider.RegisterAsync(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Deletes a resource group. Only succeeds if the resource group is empty.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the resource group contains resources.</exception>
        public async Task DeleteResourceGroupAsync(
            string subscriptionId,
            string resourceGroupName,
            CancellationToken cancellationToken = default)
        {
            // Safety check: only allow deletion of empty resource groups
            bool isEmpty = await IsResourceGroupEmptyAsync(subscriptionId, resourceGroupName, cancellationToken);
            if (!isEmpty)
            {
                throw new InvalidOperationException(
                    $"Cannot delete resource group '{resourceGroupName}' because it contains resources. " +
                    "Delete all resources first or use the Azure Portal.");
            }

            ArmClient client = GetClient(subscriptionId);
            SubscriptionResource sub = client.GetSubscriptionResource(SubscriptionResource.CreateResourceIdentifier(subscriptionId));
            ResourceGroupResource rg = (await sub.GetResourceGroupAsync(resourceGroupName, cancellationToken)).Value;

            await rg.DeleteAsync(Azure.WaitUntil.Completed, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Creates a new storage account in the specified resource group.
        /// </summary>
        /// <param name="subscriptionId">The subscription ID.</param>
        /// <param name="resourceGroupName">The resource group name.</param>
        /// <param name="accountName">The storage account name (3-24 chars, lowercase alphanumeric).</param>
        /// <param name="location">The Azure region.</param>
        /// <param name="skuName">The SKU name (e.g., "Standard_LRS", "Standard_GRS", "Premium_LRS").</param>
        /// <param name="kind">The storage account kind (default: "StorageV2").</param>
        public async Task CreateStorageAccountAsync(
            string subscriptionId,
            string resourceGroupName,
            string accountName,
            string location,
            string skuName,
            string kind = "StorageV2",
            CancellationToken cancellationToken = default)
        {
            ArmClient client = GetClient(subscriptionId);

            // Build the resource ID for the storage account
            var resourceId = ResourceIdentifier.Parse(
                $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{accountName}");

            // Create storage account using generic resource API
            // Include SKU in the data structure that ARM expects
            var data = new GenericResourceData(new Azure.Core.AzureLocation(location))
            {
                Kind = kind,
                Properties = BinaryData.FromObjectAsJson(new
                {
                    accessTier = "Hot",
                    supportsHttpsTrafficOnly = true,
                    minimumTlsVersion = "TLS1_2",
                    allowBlobPublicAccess = false
                })
            };

            // Set the SKU - GenericResourceData.Sku requires ResourcesSku
            data.Sku = new Azure.ResourceManager.Resources.Models.ResourcesSku()
            {
                Name = skuName
            };

            await client.GetGenericResources().CreateOrUpdateAsync(
                Azure.WaitUntil.Completed,
                resourceId,
                data,
                cancellationToken);
        }

        /// <summary>
        /// Creates a new Key Vault in the given resource group.
        /// </summary>
        public async Task CreateKeyVaultAsync(
            string subscriptionId,
            string resourceGroupName,
            string vaultName,
            string location,
            string skuName = "standard",
            CancellationToken cancellationToken = default)
        {
            ArmClient client = GetClient(subscriptionId);

            // Get the tenant ID for the vault's access policies
            var context = GetSubscriptionContext(subscriptionId);
            string tenantId = context?.TenantId ?? throw new InvalidOperationException("Tenant ID not available for subscription");

            // Build the resource ID for the Key Vault
            var resourceId = ResourceIdentifier.Parse(
                $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.KeyVault/vaults/{vaultName}");

            // Create Key Vault using generic resource API
            var data = new GenericResourceData(new Azure.Core.AzureLocation(location))
            {
                Properties = BinaryData.FromObjectAsJson(new
                {
                    tenantId = tenantId,
                    sku = new
                    {
                        family = "A",
                        name = skuName
                    },
                    accessPolicies = new object[] { }, // Empty - use RBAC instead
                    enableRbacAuthorization = true,
                    enableSoftDelete = true,
                    softDeleteRetentionInDays = 90,
                    enablePurgeProtection = true
                })
            };

            await client.GetGenericResources().CreateOrUpdateAsync(
                Azure.WaitUntil.Completed,
                resourceId,
                data,
                cancellationToken);
        }

        /// <summary>
        /// Deletes a storage account.
        /// </summary>
        public async Task DeleteStorageAccountAsync(
            string subscriptionId,
            string resourceGroupName,
            string storageAccountName,
            CancellationToken cancellationToken = default)
        {
            await DeleteResourceAsync(subscriptionId, resourceGroupName, "Microsoft.Storage/storageAccounts", storageAccountName, cancellationToken);
        }

        /// <summary>
        /// Deletes a Key Vault.
        /// </summary>
        public async Task DeleteKeyVaultAsync(
            string subscriptionId,
            string resourceGroupName,
            string vaultName,
            CancellationToken cancellationToken = default)
        {
            await DeleteResourceAsync(subscriptionId, resourceGroupName, "Microsoft.KeyVault/vaults", vaultName, cancellationToken);
        }

        /// <summary>
        /// Deletes a generic Azure resource by provider type.
        /// </summary>
        public async Task DeleteResourceAsync(
            string subscriptionId,
            string resourceGroupName,
            string resourceProvider,
            string resourceName,
            CancellationToken cancellationToken = default)
        {
            ArmClient client = GetClient(subscriptionId);

            // Build the resource ID
            var resourceId = ResourceIdentifier.Parse(
                $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/{resourceProvider}/{resourceName}");

            GenericResource resource = client.GetGenericResource(resourceId);
            await resource.DeleteAsync(Azure.WaitUntil.Completed, cancellationToken);
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

    /// <summary>
    /// Represents an Azure location/region.
    /// </summary>
    internal sealed class AzureLocation(string name, string displayName)
    {
        public string Name { get; } = name;
        public string DisplayName { get; } = displayName;

        public override string ToString() => DisplayName;
    }
}
