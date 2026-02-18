[marketplace]: <https://marketplace.visualstudio.com/items?itemName=MadsKristensen.AzureExplorer>
[vsixgallery]: <http://vsixgallery.com/extension/AzureExplorer.5e5465aa-805e-4395-b20d-a439f7c92ca1/>
[repo]: <https://github.com/madskristensen/AzureExplorer>

# Azure Explorer for Visual Studio

[![Build](https://github.com/madskristensen/AzureExplorer/actions/workflows/build.yaml/badge.svg)](https://github.com/madskristensen/AzureExplorer/actions/workflows/build.yaml)
![GitHub Sponsors](https://img.shields.io/github/sponsors/madskristensen)

Download this extension from the [Visual Studio Marketplace][marketplace]
or get the [CI build][vsixgallery].

--------------------------------------

**Manage your Azure resources directly from Visual Studio.** Azure Explorer provides a lightweight tool window for browsing subscriptions, resource groups, App Services, Key Vaults, Front Door profiles, and App Service Plans without leaving your IDE.

## Features

- **Browse Azure Resources** — View subscriptions, resource groups, and resources in a tree view
- **Subscription-Level Resources** — Browse App Services, Key Vaults, and Front Doors directly under subscriptions without navigating through resource groups
- **App Service Management** — Start, stop, and restart App Services with confirmation dialogs to prevent accidents
- **Key Vault Secrets** — Create, update, delete, and copy secret values directly from Visual Studio
- **Front Door Profiles** — Browse Front Door endpoints and access the Azure Portal
- **App Service Plans** — View and manage your App Service Plans
- **Quick Access** — Open the Azure Portal or Kudu console for any resource
- **Browse Sites** — Launch App Service and Front Door URLs directly in your browser
- **Streaming Logs** — View real-time HTTP logs from your App Services
- **Refresh** — Refresh subscriptions, resource groups, or individual resources

## App Service Actions

Right-click on an App Service to access these commands:

| Action      | Description                                      |
| ----------- | ------------------------------------------------ |
| **Start**   | Start a stopped App Service                      |
| **Stop**    | Stop a running App Service (with confirmation)   |
| **Restart** | Restart an App Service (with confirmation)       |
| **Browse**  | Open the App Service URL in your default browser |
| **Portal**  | Open the App Service blade in the Azure Portal   |
| **Kudu**    | Open the Kudu console for advanced diagnostics   |
| **Logs**    | Stream HTTP logs in real-time                    |

### Confirmation Dialogs

The **Stop** and **Restart** commands display confirmation dialogs to prevent accidental service disruption. This ensures you don't accidentally take down a production site with a misclick.

## Key Vault Actions

Right-click on a Key Vault or secret to access these commands:

| Action             | Description                                        |
| ------------------ | -------------------------------------------------- |
| **Add Secret**     | Create a new secret in the Key Vault               |
| **Update Secret**  | Update an existing secret's value                  |
| **Delete Secret**  | Delete a secret (with confirmation)                |
| **Copy Value**     | Copy a secret's value to the clipboard             |
| **Copy Vault URI** | Copy the Key Vault URI to the clipboard            |
| **Portal**         | Open the Key Vault blade in the Azure Portal       |

## Front Door Actions

Right-click on a Front Door profile to access these commands:

| Action     | Description                                        |
| ---------- | -------------------------------------------------- |
| **Browse** | Open the Front Door endpoint in your browser       |
| **Portal** | Open the Front Door blade in the Azure Portal      |

## App Service Plan Actions

Right-click on an App Service Plan to access these commands:

| Action     | Description                                        |
| ---------- | -------------------------------------------------- |
| **Portal** | Open the App Service Plan blade in the Azure Portal|

## Getting Started

1. Open the Azure Explorer window from **View > Azure Explorer** (next to Server Explorer)
2. Click **Sign In** to authenticate with your Azure account
3. Browse your subscriptions and resource groups
4. Right-click on App Services to manage them

## License

[Apache 2.0](LICENSE.txt)

## How can I help?

If you enjoy using the extension, please give it a ★★★★★ rating on the [Visual Studio Marketplace][marketplace].

Should you encounter bugs or have feature requests, head over to the [GitHub repo][repo] to open an issue if one doesn't already exist.

Pull requests are also very welcome, as I can't always get around to fixing all bugs myself. This is a personal passion project, so my time is limited.

Another way to help out is to [sponsor me on GitHub](https://github.com/sponsors/madskristensen).
