# Storage Accounts

Create and manage your storage resources directly from Visual Studio.

## Features

| Action | Description |
|--------|-------------|
| **Create** | Create a new storage account with performance/replication options |
| **Copy Connection String** | Quick access to storage credentials |
| **Browse Containers** | Navigate blob containers and virtual folders |
| **Browse Queues** | View storage queues and copy URLs |
| **Browse Tables** | View storage tables and copy URLs |
| **Add Tags** | Organize with resource tags |

## Creating a Storage Account

1. Expand a resource group in the tree view
2. Right-click on **Storage Accounts**
3. Select **Create Storage Account...**
4. Enter the required information:
   - **Name**: 3-24 characters, lowercase letters and numbers only (globally unique)
   - **Location**: Azure region for the storage account
   - **Performance/Replication**: Choose based on your needs
5. Click **Create**

### Naming Rules

Storage account names must:
- Be 3-24 characters long
- Contain only lowercase letters and numbers
- Be globally unique across all of Azure

### Performance & Replication Options

| Option | Best For |
|--------|----------|
| **Standard / LRS** | Dev/test, lowest cost, data replicated 3x in one datacenter |
| **Standard / GRS** | Production with geo-redundancy, data replicated to secondary region |
| **Standard / ZRS** | Production with zone redundancy, data replicated across 3 availability zones |
| **Premium / LRS** | High-performance workloads requiring SSD storage |

## Blob Containers

| Action | Description |
|--------|-------------|
| **Upload Files** | Drag and drop or select files to upload |
| **Download Blobs** | Save blobs to your local machine |
| **Delete Blobs** | Remove blobs with confirmation |
| **Copy URL** | Get the URL for any blob |

## Queues & Tables

| Action | Description |
|--------|-------------|
| **Copy URL** | Get the URL for queues or tables |
| **Open in Portal** | Jump to the resource in Azure Portal |

## Required Permissions

Storage accounts have two levels of access control:

### Management Plane (Resource Level)
| Action | Minimum Role |
|--------|--------------|
| View storage account | Reader |
| Copy connection string | Reader (uses account keys) |

### Data Plane (Blob/Queue/Table Access)
| Action | Minimum Role |
|--------|--------------|
| List/read blobs | Storage Blob Data Reader |
| Upload/delete blobs | Storage Blob Data Contributor |
| List/read queues | Storage Queue Data Reader |
| List/read tables | Storage Table Data Reader |

**Important:** Having **Reader** role on the storage account does NOT grant access to blob data. You need the specific data roles.

## Troubleshooting

### Can't see blob containers

1. **Check data plane permissions** — You need **Storage Blob Data Reader** role, not just Reader
2. **Verify firewall settings** — If the storage account has firewall rules, your IP may be blocked
3. **Private endpoints** — Storage with private endpoints may not be accessible from your network

### Upload fails

1. Verify you have **Storage Blob Data Contributor** role
2. Check if the container allows public access (for anonymous upload)
3. Ensure the blob name is valid (no invalid characters)
4. For large files, check your network connection stability

### Connection string doesn't work

1. **Account keys disabled** — Some organizations disable shared key access. Use Entra ID authentication instead.
2. **Firewall blocking** — Add your IP to the storage account's firewall allowed list
3. **Key rotation** — If keys were recently rotated, refresh Azure Explorer

### Access denied with correct role

1. **Propagation delay** — Role assignments can take up to 5 minutes to propagate
2. **Scope mismatch** — Ensure the role is assigned at the correct scope (subscription, resource group, or storage account)
3. **Conditional Access** — Your organization may have policies requiring specific conditions

## Azure Documentation

- [Storage account overview](https://learn.microsoft.com/en-us/azure/storage/common/storage-account-overview)
- [Blob storage overview](https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blobs-overview)
- [Authorize access to blobs](https://learn.microsoft.com/en-us/azure/storage/blobs/authorize-data-operations-portal)
- [Storage firewalls and virtual networks](https://learn.microsoft.com/en-us/azure/storage/common/storage-network-security)
- [Azure RBAC for storage](https://learn.microsoft.com/en-us/azure/storage/blobs/assign-azure-role-data-access)

## See Also

- [Resource Tags](../features/tags.md)
- [Troubleshooting](../troubleshooting.md)
