[marketplace]: <https://marketplace.visualstudio.com/items?itemName=MadsKristensen.AzureExplorer>
[vsixgallery]: <http://vsixgallery.com/extension/AzureExplorer.5e5465aa-805e-4395-b20d-a439f7c92ca1/>
[repo]: <https://github.com/madskristensen/AzureExplorer>

# Azure Explorer for Visual Studio

[![Build](https://github.com/madskristensen/AzureExplorer/actions/workflows/build.yaml/badge.svg)](https://github.com/madskristensen/AzureExplorer/actions/workflows/build.yaml)
![GitHub Sponsors](https://img.shields.io/github/sponsors/madskristensen)

Download from the [Visual Studio Marketplace][marketplace] or get the latest [CI build][vsixgallery].

---

**Stop context-switching.** Manage your Azure resources without ever leaving Visual Studio.

Azure Explorer brings your cloud infrastructure into your IDE with a fast, lightweight tool window. Browse subscriptions, manage App Services and Function Apps, control Virtual Machines, access Key Vault secrets, browse Storage Account blobs, connect to SQL databases, and stream live logs — all from the comfort of Visual Studio.

![Azure Explorer tool window](art/azure-explorer.png)

## Why Azure Explorer?

- **Lightweight & Fast** — No heavy SDKs or bloated dependencies, just a clean tree view
- **Instant Search** — Find any resource across all subscriptions with integrated VS search
- **Secure by Design** — Uses your existing Azure credentials with modern authentication
- **Real-time Logs** — Stream application and HTTP logs from App Services directly in VS
- **Context Menu Actions** — Right-click to start, stop, restart, browse, or open in Portal
- **VM Management** — Start, stop, and connect to Virtual Machines via RDP or SSH
- **Key Vault Integration** — Create, update, and copy secrets without touching the Portal
- **Blob Storage** — Browse containers, upload, download, and delete blobs
- **SQL Database** — Copy connection strings for quick database access

## Features

### Supported Azure Resource Types

| Resource Type | Actions |
|---------------|---------|
| **App Services** | Browse, Start/Stop/Restart, Stream Logs, Kudu, Publish Profile, App Settings |
| **Function Apps** | Browse, Start/Stop/Restart, Stream Logs, Kudu, Publish Profile, App Settings |
| **Virtual Machines** | Start/Stop/Restart, Connect via RDP/SSH, Copy IP Address |
| **Storage Accounts** | Copy Connection String, Browse Blob Containers |
| **Blob Containers** | Upload, Download, Delete, Copy URL |
| **Key Vaults** | Add/Update/Copy/Delete Secrets |
| **SQL Servers** | Copy Connection String, Browse Databases |
| **SQL Databases** | Copy Connection String |
| **Front Door** | Browse Endpoints |
| **App Service Plans** | View Hosting Plans |

### Browse Your Azure Resources

Navigate your entire Azure estate in a familiar tree view. Expand subscriptions to see resource groups, or jump straight to resources at the subscription level.

### Search Across All Subscriptions

Quickly find any resource using the integrated Visual Studio search box in the tool window. Just start typing to search across all your signed-in accounts and subscriptions simultaneously.

- **Instant Results** — Cached resources appear immediately as you type
- **Background Search** — Azure API search runs in parallel for comprehensive results
- **Grouped by Account** — Results are organized by Account → Subscription for easy navigation
- **All Resource Types** — Searches App Services, Function Apps, VMs, Storage, Key Vaults, SQL, and more

### App Service & Function App Management

Take control of your web apps and functions without leaving your code:

- **Start / Stop / Restart** — Manage app lifecycle with safety confirmations
- **Browse** — Launch your site in the default browser
- **Portal** — Jump directly to the Azure Portal blade
- **Kudu** — Access advanced diagnostics and console
- **Stream Logs** — Watch application and HTTP logs in real-time
- **Download Publish Profile** — Get deployment credentials
- **Manage App Settings** — View and edit configuration

![App Service Context Menu](art/app-service-context-menu.png)

### Storage Account & Blob Management

Browse and manage your blob storage:

- **Copy Connection String** — Quick access to storage credentials
- **Browse Containers** — Navigate blob containers and virtual folders
- **Upload Files** — Drag and drop or select files to upload
- **Download Blobs** — Save blobs to your local machine
- **Delete Blobs** — Remove blobs with confirmation
- **Copy URL** — Get the blob URL for sharing

### Key Vault Secrets

Securely manage your application secrets:

- **Add Secret** — Create new secrets directly from VS
- **Update Value** — Modify existing secret values
- **Copy Value** — One-click copy to clipboard
- **Delete** — Remove secrets with confirmation

![Key Vault Context Menu](art/key-vault-context-menu.png)

### SQL Server & Database

Connect to your Azure SQL resources:

- **Copy Connection String** — ADO.NET connection string template
- **Browse Databases** — View all databases on a server
- **Open in Portal** — Quick access to Azure Portal

### Front Door & App Service Plans

Browse Front Door profiles and endpoints, view your hosting plans, and quickly access the Portal for advanced configuration.

### Virtual Machine Management

Manage your Azure VMs without leaving Visual Studio:

- **Start / Stop / Restart** — Control VM power state with one click
- **Connect via RDP** — Launch Remote Desktop for Windows VMs
- **Connect via SSH** — Open SSH connections for Linux VMs
- **Copy IP Address** — Quick access to public IP for scripts or tools
- **View Status** — See running, stopped, or deallocated state at a glance
- **Open in Portal** — Jump to full VM management in Azure Portal

## Getting Started

1. **Install** the extension from the [Visual Studio Marketplace][marketplace]
2. **Open** Azure Explorer from **View → Azure Explorer** (next to Server Explorer)
3. **Sign in** with your Azure account
4. **Explore** your subscriptions and resources

![Welcome screen](art/welcome.png)

## Tips

- **Search** using the search box in the tool window to find resources across all subscriptions
- **Double-click** an App Service or Function App to open it in your browser
- **Double-click** a file in App Service to open it in the editor
- **Right-click** anywhere for context-specific actions
- Use the **toolbar refresh** button to sync with Azure
- **Ctrl+Alt+P** to open selected resource in Azure Portal

## Contributing

This is a passion project, and contributions are welcome!

- **Found a bug?** [Open an issue][repo]
- **Have an idea?** [Start a discussion][repo]
- **Want to contribute?** Pull requests are always welcome

If Azure Explorer saves you time, consider [rating it on the Marketplace][marketplace] or [sponsoring on GitHub](https://github.com/sponsors/madskristensen).

## 📄 License

[Apache 2.0](LICENSE.txt)
