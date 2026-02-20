[marketplace]: <https://marketplace.visualstudio.com/items?itemName=MadsKristensen.AzureExplorer>
[vsixgallery]: <http://vsixgallery.com/extension/AzureExplorer.5e5465aa-805e-4395-b20d-a439f7c92ca1/>
[repo]: <https://github.com/madskristensen/AzureExplorer>

# Azure Explorer for Visual Studio

[![Build](https://github.com/madskristensen/AzureExplorer/actions/workflows/build.yaml/badge.svg)](https://github.com/madskristensen/AzureExplorer/actions/workflows/build.yaml)
![GitHub Sponsors](https://img.shields.io/github/sponsors/madskristensen)

Download from the [Visual Studio Marketplace][marketplace] or get the latest [CI build][vsixgallery].

> **Note:** This extension only works on Visual Studio 2026 and later.

---

**Stop context-switching.**

Azure Explorer brings your cloud infrastructure into your IDE with a fast, lightweight tool window. If you miss the old **Cloud Explorer** that was removed from Visual Studio, this extension is for you. Browse subscriptions, manage App Services and Function Apps, control Virtual Machines, access Key Vault secrets, browse Storage Account blobs, connect to SQL databases, and stream live logs — all from the comfort of Visual Studio.

![Azure Explorer tool window](art/azure-explorer.png)

## Why Azure Explorer?

- **Lightweight & Fast** — No heavy SDKs or bloated dependencies, just a clean tree view
- **Instant Search** — Find any resource across all subscriptions with integrated VS search
- **Secure by Design** — Uses your existing Azure credentials with modern authentication
- **Real-time Logs** — Stream application and HTTP logs from App Services directly in VS
- **Context Menu Actions** — Right-click to start, stop, restart, browse, or open in Portal
- **VM Management** — Start, stop, and connect to Virtual Machines via RDP or SSH
- **Key Vault Integration** — Manage secrets, keys, and certificates without touching the Portal
- **Blob Storage** — Browse containers, queues, tables, upload, download, and delete blobs
- **SQL Database** — Copy connection strings for quick database access

## Supported Resources

| Resource Type | Key Actions |
|---------------|-------------|
| **App Services & Function Apps** | Browse, Start/Stop/Restart, Stream Logs, File Browser, Drag & Drop Upload |
| **Virtual Machines** | Start/Stop/Restart, Connect via RDP/SSH, Copy IP Address |
| **Storage Accounts** | Copy Connection String, Browse Blobs/Queues/Tables, Upload/Download |
| **Key Vaults** | Manage Secrets/Keys/Certificates |
| **SQL Servers & Databases** | Copy Connection String, Browse Databases |
| **Front Door** | Browse Endpoints |

📖 **[Full Documentation](https://github.com/madskristensen/AzureExplorer/blob/master/docs/index.md)** — Detailed guides for all features and resource types.

## Getting Started

1. **Install** the extension from the [Visual Studio Marketplace][marketplace]
2. **Open** Azure Explorer from **View → Azure Explorer** (next to Server Explorer)
3. **Sign in** with your Azure account using Windows native authentication
4. **Explore** your subscriptions and resources

![Welcome screen](art/welcome.png)

## Quick Tips

- **Search** with `tag:Key=Value` syntax to find resources by tag
- **Double-click** resources to open in browser, files to edit
- **Drag & drop** files onto App Services to upload
- **Ctrl+Alt+P** to open selected resource in Azure Portal

📖 See [Getting Started](https://github.com/madskristensen/AzureExplorer/blob/master/docs/getting-started.md) for more tips and detailed setup instructions.

## Contributing

This is a passion project, and contributions are welcome!

- **Found a bug?** [Open an issue][repo]
- **Have an idea?** [Start a discussion][repo]
- **Want to contribute?** Pull requests are always welcome

If Azure Explorer saves you time, consider [rating it on the Marketplace][marketplace] or [sponsoring on GitHub](https://github.com/sponsors/madskristensen).

## 📄 License

[Apache 2.0](LICENSE.txt)
