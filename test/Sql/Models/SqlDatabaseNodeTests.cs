using AzureExplorer.Sql.Models;

namespace AzureExplorer.Test.Sql.Models
{
    [TestClass]
    public sealed class SqlDatabaseNodeTests
    {
        [TestMethod]
        public void Constructor_WithOnlineStatus_SetsFormattedDescription()
        {
            // Arrange & Act
            var node = new SqlDatabaseNode("db1", "sub-123", "rg1", "sql1", "Online", "Standard", "S0");

            // Assert
            Assert.AreEqual(SqlDatabaseStatus.Online, node.Status);
            Assert.AreEqual("Standard (S0)", node.Description);
            Assert.AreEqual(PackageIds.SqlDatabaseContextMenu, node.ContextMenuId);
            Assert.IsFalse(node.SupportsChildren);
        }

        [TestMethod]
        public void Constructor_WithOfflineStatus_SetsStatusDescription()
        {
            // Arrange & Act
            var node = new SqlDatabaseNode("db1", "sub-123", "rg1", "sql1", "Offline", "Standard", "S0");

            // Assert
            Assert.AreEqual(SqlDatabaseStatus.Offline, node.Status);
            Assert.AreEqual("Offline", node.Description);
        }

        [TestMethod]
        public void ParseStatus_ReturnsExpectedStatus()
        {
            // Arrange
            var testCases = new (string Input, string Expected)[]
            {
                ("Online", "Online"),
                ("offline", "Offline"),
                ("Creating", "Creating"),
                ("paused", "Paused"),
                ("", "Unknown"),
                (null, "Unknown"),
                ("invalid", "Unknown")
            };

            // Act & Assert
            foreach ((string input, string expected) in testCases)
            {
                var actual = SqlDatabaseNode.ParseStatus(input);
                Assert.AreEqual(expected, actual.ToString());
            }
        }

        [TestMethod]
        public void Status_Set_UpdatesDescription()
        {
            // Arrange
            var node = new SqlDatabaseNode("db1", "sub-123", "rg1", "sql1", "Online", "Standard", "S0");

            // Act
            node.Status = SqlDatabaseStatus.Paused;

            // Assert
            Assert.AreEqual("Paused", node.Description);

            // Act
            node.Status = SqlDatabaseStatus.Online;

            // Assert
            Assert.AreEqual("Standard (S0)", node.Description);
        }
    }
}
