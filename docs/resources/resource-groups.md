# Resource Groups

Resource groups are containers that hold related Azure resources. Azure Explorer lets you create new resource groups and delete empty ones directly from Visual Studio.

## Features

| Action | Description |
|--------|-------------|
| **Create** | Create a new resource group with a name and location |
| **Delete** | Delete empty resource groups (safety protected) |
| **Open in Portal** | View the resource group in Azure Portal |
| **Refresh** | Reload the resource group contents |

## Creating a Resource Group

1. Expand a subscription in the tree view
2. Right-click on **Resource Groups**
3. Select **Create Resource Group...**
4. Enter a name and select a location
5. Click **Create**

The new resource group appears immediately in the tree without refreshing.

### Naming Rules

Resource group names must:
- Be 1-90 characters long
- Contain only alphanumeric characters, underscores, parentheses, hyphens, and periods
- Not end with a period

## Deleting a Resource Group

For safety, Azure Explorer only allows deletion of **empty** resource groups.

1. Right-click on the resource group
2. Select **Delete**
3. Confirm the deletion

### Why Only Empty Groups?

Deleting a resource group in Azure deletes **all resources inside it** — VMs, databases, storage accounts, everything. This is irreversible and could be catastrophic if done accidentally.

If you need to delete a non-empty resource group:
1. Delete the resources inside it first, or
2. Use the Azure Portal where additional safeguards are in place

### Error Messages

| Message | Meaning |
|---------|---------|
| *"Cannot delete resource group because it contains resources"* | The resource group has resources inside. Delete them first. |

## Context Menu

Right-click on a resource group to access:

- **Open in Portal** — View in Azure Portal
- **Refresh** — Reload contents
- **Delete** — Delete the resource group (if empty)

## Tips

- **Quick cleanup**: Create temporary resource groups for experiments, then delete them when done
- **Organization**: Use naming conventions like `rg-projectname-environment` (e.g., `rg-myapp-dev`)
- **Location matters**: Choose a location close to your users for better performance

## See Also

- [Azure Portal - Resource Groups](https://portal.azure.com/#browse/resourcegroups)
- [Azure Resource Groups documentation](https://learn.microsoft.com/en-us/azure/azure-resource-manager/management/manage-resource-groups-portal)
