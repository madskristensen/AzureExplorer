# 🎯 Seamless token refresh on auth failures

## Understanding
The Azure Explorer stops working after a while because token/credential failures during node loading result in error messages that require manual "Refresh" to recover. The fix is to add automatic retry-with-reauthentication so auth failures are transparent to the user — they just see a slightly longer "loading..." delay.

## Assumptions
- The `InteractiveBrowserCredential` with WAM should handle most token refresh scenarios automatically already
- The main failure mode is when the cached `ArmClient` or credential can't silently refresh, resulting in `AuthenticationRequiredException`, `CredentialUnavailableException`, or HTTP 401/403
- The `ReauthenticateAsync` method already exists and tries silent-first, then interactive
- A single retry after re-auth + client cache clear is sufficient (no infinite retry loops)

## Approach
Add a centralized retry helper method to `AzureResourceService` that wraps Azure operations with automatic re-authentication on auth failures. When an auth error is detected:
1. Clear the cached `ArmClient` for the subscription 
2. Clear the silent credential cache for the account
3. Attempt `ReauthenticateAsync` (tries silent first, falls back to interactive WAM prompt)
4. Retry the operation once with fresh credentials

This will be used in:
- [ExplorerNodeBase.LoadChildrenWithErrorHandlingAsync](src/Core/Models/ExplorerNodeBase.cs) — the main error handling wrapper for most nodes
- [SubscriptionResourceNodeBase.LoadChildrenAsync](src/Core/Models/SubscriptionResourceNodeBase.cs) — has its own error handling for Resource Graph + ARM fallback
- [AzureExplorerControl.TreeViewItem_Expanded](src/ToolWindows/AzureExplorerControl.xaml.cs) — catches auth errors from node expansion

The key insight: rather than modifying every call site, I'll focus on the error handling boundaries so that auth failures trigger automatic retry. The nodes that use `LoadChildrenWithErrorHandlingAsync` will benefit automatically, and `SubscriptionResourceNodeBase` and `TreeViewItem_Expanded` need specific attention since they handle errors directly.

## Key Files
- src/Core/Services/AzureResourceService.cs - add `ExecuteWithAuthRetryAsync` helper method
- src/Core/Models/ExplorerNodeBase.cs - update `LoadChildrenWithErrorHandlingAsync` to retry on auth failure
- src/Core/Models/SubscriptionResourceNodeBase.cs - update error handling in `LoadChildrenAsync` to retry
- src/ToolWindows/AzureExplorerControl.xaml.cs - update `TreeViewItem_Expanded` error handling

## Risks & Open Questions
- If the refresh token itself is expired (rare), the user will see a WAM prompt — but this is unavoidable and much better than showing an error
- Need to ensure retry only happens once to avoid infinite loops

**Progress**: 100% [██████████]

**Last Updated**: 2026-04-02 19:54:10

## 📝 Plan Steps
- ✅ **Add `ExecuteWithAuthRetryAsync` helper method to `AzureResourceService`**
- ✅ **Update `ExplorerNodeBase.LoadChildrenWithErrorHandlingAsync` to use auth retry**
- ✅ **Update `SubscriptionResourceNodeBase.LoadChildrenAsync` to use auth retry**
- ✅ **Update `AzureExplorerControl.TreeViewItem_Expanded` to use auth retry**
- ✅ **Build and verify the changes compile**

