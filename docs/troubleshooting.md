# Troubleshooting

Common issues and solutions for Azure Explorer.

## Authentication Issues

### "Sign in required" keeps appearing

**Symptoms:** You're prompted to sign in repeatedly, or authentication fails silently.

**Solutions:**
1. **Clear cached credentials:**
   - Close Visual Studio
   - Open Windows **Credential Manager** (search in Start menu)
   - Under **Windows Credentials**, remove entries containing `VS` or `Azure`
   - Restart Visual Studio and sign in again

2. **Check Windows version:** WAM (Web Account Manager) authentication requires **Windows 10 version 1903** or later.

3. **Verify account access:** Ensure your account has access to the Azure subscriptions you expect to see.

**Azure Docs:** [Troubleshoot Azure authentication](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/troubleshoot-dev-tunnels#authentication-issues)

### Wrong account or missing subscriptions

**Symptoms:** You see subscriptions from a different account, or some subscriptions are missing.

**Solutions:**
1. **Check account selector:** Click on the account in the Azure Explorer toolbar to see all signed-in accounts.

2. **Sign into additional accounts:** Use the **"Add Account"** button to sign into work, school, or personal accounts.

3. **Verify subscription access:** In the Azure Portal, confirm your account has at least **Reader** role on the subscriptions.

4. **Check hidden subscriptions:** Right-click in the tree and select **"Show Hidden"** to reveal any subscriptions you may have hidden.

**Azure Docs:** [Azure RBAC roles](https://learn.microsoft.com/en-us/azure/role-based-access-control/role-assignments-portal)

### Multi-tenant / Guest account issues

**Symptoms:** You can sign in but don't see resources from organizations where you're a guest.

**Solutions:**
1. Guest accounts may have limited visibility depending on the organization's policies.
2. Contact the organization's Azure administrator to verify your access level.
3. Try signing in directly with credentials from that organization if you have them.

**Azure Docs:** [B2B guest user access](https://learn.microsoft.com/en-us/entra/external-id/what-is-b2b)

---

## Resources Not Appearing

### Subscription appears empty

**Symptoms:** A subscription shows in the tree but has no resources under it.

**Solutions:**
1. **Refresh the view:** Click the refresh button in the toolbar or press `F5`.

2. **Check resource types:** Azure Explorer shows specific resource types. Your subscription may contain resources that aren't yet supported.

3. **Verify permissions:** You need at least **Reader** role on the subscription. Some resources may require additional permissions.

4. **Check resource groups:** Resources must be in resource groups. Expand the subscription to see all resource groups.

**Azure Docs:** [Azure built-in roles](https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles)

### Specific resource not appearing

**Symptoms:** You know a resource exists but it doesn't show in Azure Explorer.

**Solutions:**
1. **Check if resource type is supported:** See the [supported resources](index.md) list.

2. **Verify resource state:** Some resources only appear when in certain states (e.g., VMs must not be deleted).

3. **Check tags filter:** If you have an active tag filter in the search box, clear it.

4. **Refresh from Azure:** The tree view is cached for performance. Use the refresh button to fetch latest data.

---

## Performance Issues

### Slow to load subscriptions

**Symptoms:** Azure Explorer takes a long time to populate the subscription list.

**Solutions:**
1. **Hide unused subscriptions:** Right-click subscriptions you don't need and select **"Hide"**. This reduces API calls.

2. **Hide unused tenants:** If you have access to many tenants, hide the ones you don't actively use.

3. **Network connectivity:** Ensure you have a stable connection to Azure. Try accessing the Azure Portal to verify.

**Azure Docs:** [Azure service health](https://azure.microsoft.com/en-us/features/service-health/)

### Tree view feels sluggish

**Symptoms:** Expanding nodes or scrolling is slow.

**Solutions:**
1. **Collapse unused branches:** Keep only the resources you're working with expanded.

2. **Reduce visible items:** Use search to filter to specific resources instead of browsing the full tree.

3. **Check Visual Studio performance:** Other extensions or a large solution can impact overall VS performance.

---

## Resource-Specific Issues

### App Service: Can't stream logs

**Symptoms:** Log streaming shows no output or fails to connect.

**Solutions:**
1. **Enable Application Logging:**
   - In Azure Portal, go to your App Service
   - Navigate to **Monitoring → App Service logs**
   - Enable **Application Logging (Filesystem)**

2. **Check log level:** Ensure the log level is set to capture the events you expect.

3. **Generate activity:** Logs only appear when your app generates them. Make a request to your app.

**Azure Docs:** [Enable diagnostics logging for App Service](https://learn.microsoft.com/en-us/azure/app-service/troubleshoot-diagnostic-logs)

### Key Vault: Access denied

**Symptoms:** Can't view secrets, keys, or certificates in a Key Vault.

**Solutions:**
1. **Check access policy:** Your account needs explicit access to Key Vault data. Having Reader role on the Key Vault resource isn't enough.

2. **RBAC vs Access Policies:** Key Vaults can use either RBAC or vault access policies. Verify which model your vault uses and that you have appropriate permissions.

3. **Required roles for RBAC:**
   - **Key Vault Secrets User** — Read secrets
   - **Key Vault Secrets Officer** — Read/write secrets
   - Similar roles exist for keys and certificates

**Azure Docs:** [Key Vault access control](https://learn.microsoft.com/en-us/azure/key-vault/general/security-features#access-model-overview)

### Storage Account: Can't access blobs

**Symptoms:** Storage account appears but you can't browse containers or blobs.

**Solutions:**
1. **Check data plane permissions:** Resource-level Reader role doesn't grant blob access. You need:
   - **Storage Blob Data Reader** — Read blobs
   - **Storage Blob Data Contributor** — Read/write blobs

2. **Check firewall settings:** If the storage account has firewall rules, your IP may be blocked.

3. **Private endpoints:** Storage accounts with private endpoints may not be accessible from your network.

**Azure Docs:** [Authorize access to blobs](https://learn.microsoft.com/en-us/azure/storage/blobs/authorize-data-operations-portal)

### Virtual Machine: RDP/SSH fails

**Symptoms:** Clicking Connect via RDP or SSH doesn't work.

**Solutions:**
1. **Check VM is running:** The VM must be in **Running** state.

2. **Verify public IP:** The VM needs a public IP address, or you need network connectivity to its private IP.

3. **Check NSG rules:** Network Security Groups must allow inbound RDP (port 3389) or SSH (port 22).

4. **RDP client:** Ensure you have Remote Desktop Connection installed (built into Windows).

5. **SSH client:** For Linux VMs, ensure you have an SSH client. Windows 10+ includes OpenSSH by default.

**Azure Docs:** [Troubleshoot RDP connections](https://learn.microsoft.com/en-us/troubleshoot/azure/virtual-machines/windows/troubleshoot-rdp-connection)

---

## Still Having Issues?

If you've tried the solutions above and still have problems:

1. **Check GitHub Issues:** Search [existing issues](https://github.com/madskristensen/AzureExplorer/issues) for similar problems.

2. **Open a new issue:** Include:
   - Visual Studio version
   - Azure Explorer version
   - Steps to reproduce
   - Any error messages

3. **Enable diagnostic logging:** In Visual Studio, go to **Tools → Options → Azure Explorer → Enable Diagnostic Logging**, reproduce the issue, and attach the log file to your issue.
