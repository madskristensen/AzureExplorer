using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;

namespace AzureExplorer.Services
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
            TokenCredential credential = AzureAuthService.Instance.Credential
                ?? throw new InvalidOperationException("Not signed in to Azure.");

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
        }

        /// <summary>
        /// Yields Azure subscriptions across all accessible tenants as they are discovered.
        /// Results stream incrementally so callers can update the UI per-item.
        /// </summary>
        public async IAsyncEnumerable<SubscriptionResource> GetSubscriptionsAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            TokenCredential credential = AzureAuthService.Instance.Credential
                ?? throw new InvalidOperationException("Not signed in to Azure.");

            var client = new ArmClient(credential);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Enumerate tenants and list subscriptions per tenant.
            // GET /subscriptions is cross-tenant (returns ALL user subscriptions
            // regardless of which tenant issued the token), so we filter by the
            // subscription's owning TenantId to avoid duplicates.
            await foreach (TenantResource tenant in client.GetTenants().GetAllAsync(cancellationToken))
            {
                Guid? tenantGuid = tenant.Data.TenantId;
                if (!tenantGuid.HasValue)
                    continue;

                var tenantId = tenantGuid.Value.ToString();
                var tenantCredential = new TenantScopedCredential(credential, tenantId);
                var tenantClient = new ArmClient(tenantCredential);

                IAsyncEnumerator<SubscriptionResource> enumerator = null;
                try
                {
                    enumerator = tenantClient.GetSubscriptions().GetAllAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Skipping tenant {tenantId}: {ex.Message}");
                    continue;
                }

                if (enumerator != null)
                {
                    try
                    {
                        while (true)
                        {
                            bool moved;
                            try
                            {
                                moved = await enumerator.MoveNextAsync();
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Skipping tenant {tenantId}: {ex.Message}");
                                break;
                            }

                            if (!moved)
                                break;

                            SubscriptionResource sub = enumerator.Current;

                            if (sub.Data.TenantId.HasValue && sub.Data.TenantId.Value != tenantGuid.Value)
                                continue;

                            if (seen.Add(sub.Data.SubscriptionId))
                            {
                                _subscriptionTenantMap[sub.Data.SubscriptionId] = tenantId;
                                yield return sub;
                            }
                        }
                    }
                    finally
                    {
                        await enumerator.DisposeAsync();
                    }
                }
            }
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
        /// Wraps a <see cref="TokenCredential"/> to inject a specific tenant ID
        /// into every token request, enabling multi-tenant token acquisition.
        /// </summary>
        private sealed class TenantScopedCredential(TokenCredential inner, string tenantId) : TokenCredential
        {
            public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
            {
                var scoped = new TokenRequestContext(
                    requestContext.Scopes,
                    requestContext.ParentRequestId,
                    requestContext.Claims,
                    tenantId);
                return inner.GetToken(scoped, cancellationToken);
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
    }
}
