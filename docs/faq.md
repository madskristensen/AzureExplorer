# Frequently Asked Questions

Quick answers to common questions about Azure Explorer.

---

## General

### What is Azure Explorer?

Azure Explorer is a Visual Studio extension that lets you browse and manage Azure resources directly from within the IDE. It's a modern replacement for the deprecated Cloud Explorer that was removed from Visual Studio.

### Is Azure Explorer free?

Yes, Azure Explorer is completely free and open source under the Apache 2.0 license.

### Who makes Azure Explorer?

Azure Explorer is created by [Mads Kristensen](https://github.com/madskristensen), a developer at Microsoft and author of many popular Visual Studio extensions including Web Essentials, Markdown Editor, and 100+ others.

### What versions of Visual Studio are supported?

Azure Explorer requires **Visual Studio 2026** or later. It's not available for earlier versions due to dependencies on modern authentication APIs.

---

## Accounts & Authentication

### Which Azure accounts are supported?

- ✅ Personal Microsoft accounts (outlook.com, hotmail.com)
- ✅ Work or school accounts (Entra ID / Azure AD)
- ✅ Accounts with access to multiple tenants
- ✅ Guest accounts (B2B)
- ❌ Azure Government (planned)
- ❌ Azure China 21Vianet (planned)

See [Authentication](authentication.md) for details.

### Can I use multiple Azure accounts?

Yes! Click the account icon in the toolbar and select "Add Account" to sign into additional accounts. Resources from all accounts appear together in the tree view.

### Why am I being asked to sign in repeatedly?

This usually indicates a credential caching issue. Try:
1. Close Visual Studio
2. Clear Azure credentials from Windows Credential Manager
3. Restart Visual Studio and sign in again

See [Troubleshooting](troubleshooting.md#authentication-issues) for detailed steps.

### Is my data secure?

Yes. Azure Explorer:
- Never stores passwords or tokens on disk
- Uses Windows native authentication (WAM)
- Communicates directly with Azure APIs over HTTPS
- Never sends data to third-party services

See [Authentication - Privacy & Security](authentication.md#privacy--security) for details.

---

## Resources & Features

### Why don't I see all my subscriptions?

Common reasons:
1. **Wrong account** — Check you're signed into the correct account
2. **Hidden subscriptions** — Right-click and select "Show Hidden"
3. **Insufficient permissions** — You need at least Reader role
4. **Multiple tenants** — Some subscriptions may be in different tenants

### Why is a specific resource not appearing?

1. **Unsupported resource type** — Check the [supported resources](index.md#resource-types) list
2. **Insufficient permissions** — You need Reader role on the resource
3. **Cached data** — Press F5 or click Refresh to fetch latest data
4. **Active filter** — Clear any search/tag filters

### What Azure resource types are supported?

Currently supported:
- App Services & Function Apps
- Virtual Machines
- Storage Accounts (Blobs, Queues, Tables)
- Key Vaults (Secrets, Keys, Certificates)
- SQL Servers & Databases
- Front Door

More resource types are planned. [Request a resource type](https://github.com/madskristensen/AzureExplorer/issues) on GitHub!

### Can I add/create new Azure resources?

Azure Explorer is focused on **browsing and managing** existing resources. Creating new resources should be done through:
- Azure Portal
- Azure CLI
- Visual Studio's Publish dialog
- Infrastructure as Code (Bicep, Terraform)

### Can I deploy my application from Azure Explorer?

Azure Explorer supports uploading files to App Services via drag-and-drop, but for full deployment workflows, use:
- Visual Studio's **Publish** feature
- Azure DevOps / GitHub Actions
- Azure CLI

---

## Key Vault

### Why can't I see Key Vault secrets?

Having **Reader** role on the Key Vault resource isn't enough. You need data plane permissions:
- **Key Vault Secrets User** — To read secrets
- **Key Vault Secrets Officer** — To read and write secrets

See [Key Vault troubleshooting](troubleshooting.md#key-vault-access-denied).

### Is it safe to view secrets in Azure Explorer?

Yes, but use caution:
- Secrets are fetched on-demand only when you explicitly request them
- Copied secrets go to your clipboard (clear it when done)
- Azure Explorer never logs or stores secret values

---

## Storage

### Why can't I access blob containers?

Storage accounts require separate **data plane** permissions:
- **Storage Blob Data Reader** — To read blobs
- **Storage Blob Data Contributor** — To read/write blobs

Resource-level Reader role doesn't grant blob access.

### Can I upload large files?

Yes, but for very large files or bulk uploads, consider using:
- [Azure Storage Explorer](https://azure.microsoft.com/en-us/products/storage/storage-explorer/) (standalone app)
- [AzCopy](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azcopy-v10) (command line)

---

## Virtual Machines

### Why won't RDP/SSH connect?

Common issues:
1. **VM not running** — Start the VM first
2. **No public IP** — VM needs a public IP or you need VPN/ExpressRoute access
3. **Firewall/NSG** — Inbound rules must allow RDP (3389) or SSH (22)

See [VM troubleshooting](troubleshooting.md#virtual-machine-rdpssh-fails).

---

## Performance

### Azure Explorer is slow to load. What can I do?

1. **Hide unused subscriptions/tenants** — Reduces API calls
2. **Collapse unused branches** — Keeps the tree lightweight
3. **Use search** — Filter to specific resources instead of browsing everything

### Does Azure Explorer cache data?

Yes, resource metadata is cached for performance. Use the **Refresh** button (F5) to fetch the latest data from Azure.

---

## Comparison

### How is this different from Cloud Explorer?

Azure Explorer is a spiritual successor to Cloud Explorer with:
- ✅ Modern authentication (WAM)
- ✅ Better performance
- ✅ More resource types
- ✅ Active development and support
- ✅ Open source

### How is this different from Azure Portal?

| | Azure Explorer | Azure Portal |
|---|---|---|
| **Speed** | Faster for common tasks | Full-featured but heavier |
| **Context** | Stays in VS | Separate browser tab |
| **Features** | Focused on dev scenarios | Everything |
| **Create resources** | No | Yes |

Use Azure Explorer for quick lookups and common actions. Use Azure Portal for complex configuration.

### How is this different from Azure CLI?

Azure Explorer provides a visual interface for browsing. Azure CLI is better for:
- Scripting and automation
- Bulk operations
- CI/CD pipelines

They complement each other well!

---

## Contributing & Support

### How do I report a bug?

[Open an issue on GitHub](https://github.com/madskristensen/AzureExplorer/issues) with:
- Visual Studio version
- Azure Explorer version
- Steps to reproduce
- Error messages (if any)

### How do I request a feature?

[Start a discussion on GitHub](https://github.com/madskristensen/AzureExplorer/discussions) or open an issue tagged as a feature request.

### Can I contribute code?

Yes! Azure Explorer is open source. Pull requests are welcome. Check the [repository](https://github.com/madskristensen/AzureExplorer) for contribution guidelines.

---

## See Also

- [Getting Started](getting-started.md)
- [Authentication](authentication.md)
- [Troubleshooting](troubleshooting.md)
- [Full Documentation](index.md)
