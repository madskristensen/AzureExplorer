# Authentication

Azure Explorer uses Windows native authentication (WAM) for secure, seamless sign-in to your Azure accounts.

## Supported Account Types

| Account Type | Supported | Notes |
|--------------|-----------|-------|
| **Personal Microsoft Account** | ✅ | outlook.com, hotmail.com, live.com |
| **Work or School (Entra ID)** | ✅ | Organizational accounts |
| **Multiple Tenants** | ✅ | Access resources across tenants |
| **Guest Accounts (B2B)** | ✅ | With organization permissions |
| **Azure Government** | ❌ | Planned for future release |
| **Azure China (21Vianet)** | ❌ | Planned for future release |

## How Authentication Works

Azure Explorer uses **Web Account Manager (WAM)**, the same authentication system used by Windows, Microsoft Office, and Visual Studio itself. This provides:

- **Single Sign-On** — Uses your existing Windows/Visual Studio Azure credentials
- **No stored passwords** — Credentials are managed by Windows, not the extension
- **Modern authentication** — Supports MFA, Conditional Access, and other Entra ID policies
- **Token refresh** — Automatic token refresh without re-prompting

**Azure Docs:** [Microsoft identity platform authentication](https://learn.microsoft.com/en-us/entra/identity-platform/authentication-vs-authorization)

## Signing In

### First Time Setup

1. Open Azure Explorer via **View → Azure Explorer**
2. Click **"Sign in to Azure"** in the welcome screen
3. Windows will show the account picker — select or add your Azure account
4. Grant consent if prompted (first time only)
5. Your subscriptions appear automatically

### Adding Additional Accounts

You can sign into multiple Azure accounts simultaneously:

1. Click the **account icon** in the Azure Explorer toolbar
2. Select **"Add Account"**
3. Sign in with your additional account
4. Resources from all accounts appear in the tree

### Switching Between Accounts

All signed-in accounts are shown together in the tree view. Each account's subscriptions are grouped under the account name.

To focus on specific accounts:
- **Hide accounts** you don't need by right-clicking → **Hide**
- Use **"Show Hidden"** in the toolbar to reveal hidden accounts

## Multi-Tenant Access

If your account has access to multiple Entra ID tenants (organizations), Azure Explorer automatically discovers and shows resources from all accessible tenants.

### How It Works

1. When you sign in, Azure queries all tenants your account can access
2. Subscriptions from each tenant appear under your account
3. Tenant name is shown in the subscription tooltip

### Guest Access (B2B)

If you're a guest in another organization:

1. Sign in with your home account
2. Guest tenant subscriptions appear automatically (if the organization allows)
3. Some features may be limited based on guest policies

**Note:** Guest access depends on the host organization's Entra ID configuration. Contact their administrator if you can't see expected resources.

**Azure Docs:** [B2B collaboration overview](https://learn.microsoft.com/en-us/entra/external-id/what-is-b2b)

## Required Permissions

### Minimum Permissions

To see and browse resources, you need at least **Reader** role at the subscription or resource group level.

### Recommended Permissions

| Action | Required Role |
|--------|---------------|
| View resources | Reader |
| Start/Stop App Services | Contributor |
| Start/Stop VMs | Virtual Machine Contributor |
| Read Key Vault secrets | Key Vault Secrets User |
| Modify Key Vault secrets | Key Vault Secrets Officer |
| Read Storage blobs | Storage Blob Data Reader |
| Upload/delete blobs | Storage Blob Data Contributor |

**Azure Docs:** [Azure built-in roles](https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles)

## Troubleshooting Authentication

### Clear Cached Credentials

If you're experiencing sign-in issues:

1. Close Visual Studio completely
2. Open **Credential Manager** (Windows search → "Credential Manager")
3. Under **Windows Credentials**, find and remove entries containing:
   - `VS CodeSpaces`
   - `VSToken`
   - `Azure`
   - `microsoft.com`
4. Restart Visual Studio and sign in again

### Token Expiration

Tokens automatically refresh in the background. If you see authentication errors:

1. Try refreshing the Azure Explorer view (F5 or toolbar button)
2. If issues persist, sign out and sign back in
3. Check if your organization has Conditional Access policies that may require re-authentication

### Conditional Access & MFA

Azure Explorer fully supports:

- **Multi-Factor Authentication (MFA)**
- **Conditional Access policies**
- **Device compliance requirements**

If your organization enforces these, you'll see the appropriate prompts during sign-in.

**Azure Docs:** [Conditional Access overview](https://learn.microsoft.com/en-us/entra/identity/conditional-access/overview)

## Privacy & Security

### What Azure Explorer Accesses

- **Subscription list** — To show your Azure subscriptions
- **Resource metadata** — Names, types, locations, tags
- **Resource operations** — Start/stop, configuration (only when you initiate)
- **Secrets/keys** — Only when you explicitly request to view them

### What Azure Explorer Does NOT Do

- ❌ Store credentials or tokens on disk
- ❌ Send data to any third-party services
- ❌ Access resources without your explicit action
- ❌ Modify resources without confirmation prompts

### Data Flow

```
Your PC → Azure APIs (management.azure.com, vault.azure.net, etc.)
         ↑
    WAM handles auth tokens
    (managed by Windows)
```

All communication is directly between your machine and Azure APIs over HTTPS.

## See Also

- [Troubleshooting](troubleshooting.md) — Common issues and solutions
- [Getting Started](getting-started.md) — Initial setup guide
- [Azure RBAC documentation](https://learn.microsoft.com/en-us/azure/role-based-access-control/overview)
