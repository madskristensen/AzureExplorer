using System;
using System.Threading.Tasks;
using System.Windows;

using AzureExplorer.Sql.Models;
using AzureExplorer.ToolWindows;

namespace AzureExplorer.Sql.Commands
{
    [Command(PackageIds.CopySqlConnectionString)]
    internal sealed class CopySqlConnectionStringCommand : BaseCommand<CopySqlConnectionStringCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            string serverName = null;
            string databaseName = null;

            // Handle both SQL Server and SQL Database nodes
            if (AzureExplorerControl.SelectedNode is SqlDatabaseNode dbNode)
            {
                serverName = dbNode.ServerName;
                databaseName = dbNode.Label;
            }
            else if (AzureExplorerControl.SelectedNode is SqlServerNode serverNode)
            {
                serverName = serverNode.FullyQualifiedDomainName ?? $"{serverNode.Label}.database.windows.net";
                databaseName = "master"; // Default to master for server-level connection
            }
            else
            {
                return;
            }

            try
            {
                // Build ADO.NET connection string template
                // Note: Does not include credentials - user must supply their own
                string connectionString = BuildConnectionString(serverName, databaseName);

                Clipboard.SetText(connectionString);
                await VS.StatusBar.ShowMessageAsync($"Connection string copied (credentials required)");
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                await VS.StatusBar.ShowMessageAsync($"Error: {ex.Message}");
            }
        }

        private static string BuildConnectionString(string serverName, string databaseName)
        {
            // Ensure server name is fully qualified
            if (!serverName.Contains("."))
            {
                serverName = $"{serverName}.database.windows.net";
            }

            // ADO.NET connection string template for Azure SQL
            // User must fill in User ID and Password, or use Azure AD authentication
            return $"Server=tcp:{serverName},1433;Initial Catalog={databaseName};Persist Security Info=False;User ID=<username>;Password=<password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        }
    }
}
