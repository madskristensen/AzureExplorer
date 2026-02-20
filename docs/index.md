# Azure Explorer Documentation

Welcome to the Azure Explorer documentation. Browse the sections below to learn about specific features and resource types.

## Getting Started

- [Installation & Setup](getting-started.md) — Requirements, installation, and first steps
- [Authentication](authentication.md) — Account types, sign-in, and permissions
- [FAQ](faq.md) — Frequently asked questions
- [Troubleshooting](troubleshooting.md) — Common issues and solutions

## Features

Cross-cutting features that work across all resource types:

- [Search](features/search.md) — Find resources across all subscriptions
- [Resource Tags](features/tags.md) — Organize and filter resources
- [Activity Log](features/activity-log.md) — Track your actions
- [File Browser](features/file-browser.md) — Browse and upload files to App Services

## Resource Types

Detailed guides for each supported Azure resource:

| Resource | Description |
|----------|-------------|
| [Resource Groups](resources/resource-groups.md) | Create and manage resource containers |
| [App Services & Function Apps](resources/app-services.md) | Web apps, APIs, and serverless functions |
| [Virtual Machines](resources/virtual-machines.md) | VMs with RDP/SSH connectivity |
| [Storage Accounts](resources/storage-accounts.md) | Blobs, queues, and tables |
| [Key Vaults](resources/key-vaults.md) | Secrets, keys, and certificates |
| [SQL Databases](resources/sql-databases.md) | Azure SQL servers and databases |
| [Front Door](resources/front-door.md) | CDN and global load balancing |

## Quick Reference

| Resource Type         | Key Actions |
| --------------------- | ----------- |
| **Resource Groups**   | Create, Delete (if empty), Open in Portal |
| **App Services**      | Browse, Start/Stop/Restart, Stream Logs, File Browser, Drag & Drop Upload |
| **Function Apps**     | Browse, Start/Stop/Restart, Stream Logs, File Browser, Drag & Drop Upload |
| **Virtual Machines**  | Start/Stop/Restart, Connect via RDP/SSH, Copy IP Address |
| **Storage Accounts**  | Copy Connection String, Browse Blobs/Queues/Tables |
| **Blob Containers**   | Upload, Download, Delete, Copy URL |
| **Key Vaults**        | Browse Secrets/Keys/Certificates |
| **Secrets**           | Add, Update, Copy Value, Delete |
| **SQL Servers**       | Copy Connection String, Browse Databases |
| **Front Door**        | Browse Endpoints |

## Azure Documentation

Official Microsoft documentation for Azure services:

- [Azure Portal](https://portal.azure.com)
- [Azure RBAC overview](https://learn.microsoft.com/en-us/azure/role-based-access-control/overview)
- [Entra ID (Azure AD)](https://learn.microsoft.com/en-us/entra/fundamentals/)
- [App Service docs](https://learn.microsoft.com/en-us/azure/app-service/)
- [Key Vault docs](https://learn.microsoft.com/en-us/azure/key-vault/)
- [Storage docs](https://learn.microsoft.com/en-us/azure/storage/)
- [Virtual Machines docs](https://learn.microsoft.com/en-us/azure/virtual-machines/)
- [Azure SQL docs](https://learn.microsoft.com/en-us/azure/azure-sql/)

## Contributing

- **Found a bug?** [Open an issue](https://github.com/madskristensen/AzureExplorer)
- **Have an idea?** [Start a discussion](https://github.com/madskristensen/AzureExplorer)
- **Want to contribute?** Pull requests are always welcome
