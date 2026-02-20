# SQL Servers & Databases

Connect to your Azure SQL resources.

## SQL Servers

| Action | Description |
|--------|-------------|
| **Copy Connection String** | ADO.NET connection string template |
| **Browse Databases** | View all databases on a server |
| **Open in Portal** | Quick access to Azure Portal |
| **Add Tags** | Organize with resource tags |

## SQL Databases

| Action | Description |
|--------|-------------|
| **Copy Connection String** | ADO.NET connection string for the database |

## Connection Strings

Azure Explorer provides connection string templates. You'll need to:

1. **Copy the connection string** from Azure Explorer
2. **Replace the password placeholder** with your actual password
3. **Optionally adjust settings** like timeout, encryption, etc.

**Example connection string:**
```
Server=tcp:myserver.database.windows.net,1433;Initial Catalog=mydb;Persist Security Info=False;User ID=myuser;Password={your_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

**Tip:** For production, consider using managed identities instead of SQL authentication.

## Required Permissions

| Action | Minimum Role |
|--------|--------------|
| View SQL Server | Reader |
| View databases | Reader |
| Copy connection string | Reader |
| Query data | SQL Server role (db_datareader, etc.) |

**Note:** Azure RBAC roles (Reader, Contributor) control access to the **resource**. To query data, you need **SQL Server roles** assigned within the database itself.

## Troubleshooting

### Can't connect to database

1. **Firewall rules** — Add your client IP to the SQL Server firewall:
   - Azure Portal → SQL Server → **Networking**
   - Add your client IP or enable "Allow Azure services"

2. **Wrong credentials** — Verify username and password

3. **Database offline** — Check database status in Azure Portal

4. **Connection timeout** — Try increasing the timeout value in the connection string

### Connection string doesn't work

1. **Replace password placeholder** — The template includes `{your_password}` placeholder
2. **Check server name** — Ensure it ends with `.database.windows.net`
3. **Port blocked** — Ensure outbound port 1433 is open on your network
4. **TLS version** — Azure SQL requires TLS 1.2; older clients may fail

### Can't see all databases

1. **Permissions** — You need Reader role on each database, or on the SQL Server
2. **Hidden system databases** — Some system databases aren't shown by default
3. **Refresh** — Press F5 to refresh the view

### Entra ID authentication

If your organization uses Entra ID (Azure AD) for SQL authentication:

1. The connection string format differs from SQL authentication
2. You may need additional drivers (Microsoft.Data.SqlClient)
3. Check with your DBA for the correct connection method

## Azure Documentation

- [Azure SQL Database overview](https://learn.microsoft.com/en-us/azure/azure-sql/database/sql-database-paas-overview)
- [Connect to Azure SQL](https://learn.microsoft.com/en-us/azure/azure-sql/database/connect-query-content-reference-guide)
- [Firewall rules](https://learn.microsoft.com/en-us/azure/azure-sql/database/firewall-configure)
- [Entra ID authentication](https://learn.microsoft.com/en-us/azure/azure-sql/database/authentication-aad-overview)
- [Connection string formats](https://learn.microsoft.com/en-us/azure/azure-sql/database/connect-query-dotnet-core)

## See Also

- [Resource Tags](../features/tags.md)
- [Troubleshooting](../troubleshooting.md)
