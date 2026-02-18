[marketplace]: <https://marketplace.visualstudio.com/items?itemName=MadsKristensen.AzureExplorer>
[vsixgallery]: <http://vsixgallery.com/extension/AzureExplorer.5e5465aa-805e-4395-b20d-a439f7c92ca1/>
[repo]: <https://github.com/madskristensen/AzureExplorer>

# Azure Explorer for Visual Studio

[![Build](https://github.com/madskristensen/AzureExplorer/actions/workflows/build.yaml/badge.svg)](https://github.com/madskristensen/AzureExplorer/actions/workflows/build.yaml)
![GitHub Sponsors](https://img.shields.io/github/sponsors/madskristensen)

Download from the [Visual Studio Marketplace][marketplace] or get the latest [CI build][vsixgallery].

---

**Stop context-switching.** Manage your Azure resources without ever leaving Visual Studio.

Azure Explorer brings your cloud infrastructure into your IDE with a fast, lightweight tool window. Browse subscriptions, manage App Services, access Key Vault secrets, and stream live logs — all from the comfort of Visual Studio.

![Azure Explorer tool window](art/azure-explorer.png)

## Why Azure Explorer?

- **Lightweight & Fast** — No heavy SDKs or bloated dependencies, just a clean tree view
- **Secure by Design** — Uses your existing Azure credentials with modern authentication
- **Real-time Logs** — Stream HTTP logs from App Services directly in VS
- **Context Menu Actions** — Right-click to start, stop, restart, browse, or open in Portal
- **Key Vault Integration** — Create, update, and copy secrets without touching the Portal

## Features

### Browse Your Azure Resources

Navigate your entire Azure estate in a familiar tree view. Expand subscriptions to see resource groups, or jump straight to App Services, Key Vaults, and Front Doors at the subscription level.

<!-- TODO: Add screenshot showing the tree view with expanded subscriptions -->

### App Service Management

Take control of your web apps without leaving your code:

- **Start / Stop / Restart** — Manage app lifecycle with safety confirmations
- **Browse** — Launch your site in the default browser
- **Portal** — Jump directly to the Azure Portal blade
- **Kudu** — Access advanced diagnostics and console
- **Stream Logs** — Watch HTTP logs in real-time

![App Service Context Menu](art/app-service-context-menu.png)

### Key Vault Secrets

Securely manage your application secrets:

- **Add Secret** — Create new secrets directly from VS
- **Update Value** — Modify existing secret values
- **Copy Value** — One-click copy to clipboard
- **Delete** — Remove secrets with confirmation

![Key Vault Context Menu](art/key-vault-context-menu.png)

### Front Door & App Service Plans

Browse Front Door profiles and endpoints, view your hosting plans, and quickly access the Portal for advanced configuration.

## Getting Started

1. **Install** the extension from the [Visual Studio Marketplace][marketplace]
2. **Open** Azure Explorer from **View → Azure Explorer** (next to Server Explorer)
3. **Sign in** with your Azure account
4. **Explore** your subscriptions and resources

![Welcome screen](art/welcome.png)

## Tips

- **Double-click** an App Service to open it in your browser
- **Double-click** a file in App Service to open it in the editor
- **Right-click** anywhere for context-specific actions
- Use the **toolbar refresh** button to sync with Azure

## Contributing

This is a passion project, and contributions are welcome!

- **Found a bug?** [Open an issue][repo]
- **Have an idea?** [Start a discussion][repo]
- **Want to contribute?** Pull requests are always welcome

If Azure Explorer saves you time, consider [rating it on the Marketplace][marketplace] or [sponsoring on GitHub](https://github.com/sponsors/madskristensen).

## 📄 License

[Apache 2.0](LICENSE.txt)
