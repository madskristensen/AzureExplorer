# Key Vaults

Securely manage your secrets, keys, and certificates.

![Key Vault Context Menu](../../art/key-vault-context-menu.png)

## Secrets

| Action | Description |
|--------|-------------|
| **Add Secret** | Create new secrets directly from VS |
| **Update Value** | Modify existing secret values |
| **Copy Value** | One-click copy to clipboard |
| **Delete** | Remove secrets with confirmation |

## Keys

| Action | Description |
|--------|-------------|
| **Copy Key ID** | Copy the full key identifier URL |
| **Open in Portal** | Jump to the key in Azure Portal |

## Certificates

| Action | Description |
|--------|-------------|
| **Copy Certificate ID** | Copy the full certificate identifier URL |
| **View Expiration** | See certificate expiry dates at a glance |
| **Open in Portal** | Jump to the certificate in Azure Portal |

## Required Permissions

Key Vault uses a separate permission model from Azure RBAC. You need **data plane** permissions to access secrets, keys, and certificates.

### RBAC Model (Recommended)

| Action | Minimum Role |
|--------|--------------|
| View Key Vault resource | Reader |
| List/read secrets | Key Vault Secrets User |
| Create/update/delete secrets | Key Vault Secrets Officer |
| List/read keys | Key Vault Crypto User |
| Manage keys | Key Vault Crypto Officer |
| List/read certificates | Key Vault Certificates User |
| Manage certificates | Key Vault Certificates Officer |
| Full access | Key Vault Administrator |

### Access Policy Model (Legacy)

If your Key Vault uses access policies instead of RBAC:
1. Go to the Key Vault in Azure Portal
2. Navigate to **Access policies**
3. Add a policy granting your account the needed permissions (Get, List, Set, Delete)

**Tip:** Check which model your vault uses: Azure Portal → Key Vault → **Access configuration**

## Security Best Practices

- **Clear clipboard** after copying secrets
- **Avoid logging** secret values
- **Use managed identities** for applications instead of copying secrets
- **Enable soft delete** on Key Vaults to prevent accidental loss
- **Audit access** via Key Vault diagnostic logs

## Troubleshooting

### Access denied to secrets

1. **Check permission model** — Is the vault using RBAC or Access Policies?
2. **Verify role assignment** — You need **Key Vault Secrets User**, not just Reader
3. **Check scope** — The role must be assigned on the Key Vault, not just the subscription
4. **Propagation delay** — New role assignments can take up to 5 minutes

### Can't add or update secrets

1. You need **Key Vault Secrets Officer** role (not just User)
2. Check if the vault has **purge protection** enabled (affects delete operations)
3. Verify the secret name is valid (alphanumeric, dashes allowed)

### Key Vault not appearing

1. Verify you have at least **Reader** role on the Key Vault resource
2. Check if the vault is in a hidden subscription
3. Refresh the Azure Explorer view

### Firewall blocking access

If your Key Vault has firewall rules enabled:
1. Add your IP address to the allowed list in Azure Portal
2. Or use a private endpoint if your organization requires it

## Azure Documentation

- [Key Vault overview](https://learn.microsoft.com/en-us/azure/key-vault/general/overview)
- [Key Vault access control](https://learn.microsoft.com/en-us/azure/key-vault/general/security-features)
- [RBAC for Key Vault](https://learn.microsoft.com/en-us/azure/key-vault/general/rbac-guide)
- [Key Vault best practices](https://learn.microsoft.com/en-us/azure/key-vault/general/best-practices)
- [Secrets management](https://learn.microsoft.com/en-us/azure/key-vault/secrets/about-secrets)

## See Also

- [Resource Tags](../features/tags.md)
- [Troubleshooting](../troubleshooting.md)
- [Authentication](../authentication.md)
