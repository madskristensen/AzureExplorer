using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        /// Queries tenants in parallel for faster loading.
        /// </summary>
        public async IAsyncEnumerable<SubscriptionResource> GetSubscriptionsAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            TokenCredential credential = AzureAuthService.Instance.Credential
                ?? throw new InvalidOperationException("Not signed in to Azure.");

            var client = new ArmClient(credential);
            var seen = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

            // First, collect all tenants
            var tenants = new List<TenantResource>();
            await foreach (TenantResource tenant in client.GetTenants().GetAllAsync(cancellationToken))
            {
                if (tenant.Data.TenantId.HasValue)
                    tenants.Add(tenant);
            }

            // Query all tenants in parallel, collecting results into a concurrent bag
            var results = new ConcurrentBag<(SubscriptionResource Sub, string TenantId)>();

            var tasks = tenants.Select(tenant => Task.Run(async () =>
            {
                var tenantId = tenant.Data.TenantId.Value.ToString();
                try
                {
                    var tenantCredential = new TenantScopedCredential(credential, tenantId);
                    var tenantClient = new ArmClient(tenantCredential);

                    await foreach (SubscriptionResource sub in tenantClient.GetSubscriptions().GetAllAsync(cancellationToken))
                    {
                        // Filter to subscriptions owned by this tenant to avoid duplicates
                        if (sub.Data.TenantId.HasValue && sub.Data.TenantId.Value != tenant.Data.TenantId.Value)
                            continue;

                        results.Add((sub, tenantId));
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Skipping tenant {tenantId}: {ex.Message}");
                }
            }, cancellationToken)).ToArray();

            await Task.WhenAll(tasks);

            // Yield unique subscriptions
            foreach (var (sub, tenantId) in results)
            {
                if (seen.TryAdd(sub.Data.SubscriptionId, 0))
                {
                    _subscriptionTenantMap[sub.Data.SubscriptionId] = tenantId;
                    yield return sub;
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
