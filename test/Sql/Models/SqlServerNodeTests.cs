using AzureExplorer.Sql.Models;

namespace AzureExplorer.Test.Sql.Models
{
    [TestClass]
    public sealed class SqlServerNodeTests
    {
        [TestMethod]
        public void Constructor_WithReadyState_SetsFqdnDescription()
        {
            // Arrange & Act
            var node = new SqlServerNode("sql1", "sub-123", "rg1", "Ready", "sql1.database.windows.net");

            // Assert
            Assert.AreEqual(SqlServerState.Ready, node.State);
            Assert.AreEqual("sql1.database.windows.net", node.Description);
            Assert.AreEqual(PackageIds.SqlServerContextMenu, node.ContextMenuId);
            Assert.IsTrue(node.SupportsChildren);
        }

        [TestMethod]
        public void Constructor_WithUnavailableState_SetsUnavailableDescription()
        {
            // Arrange & Act
            var node = new SqlServerNode("sql1", "sub-123", "rg1", "Unavailable", "sql1.database.windows.net");

            // Assert
            Assert.AreEqual(SqlServerState.Unavailable, node.State);
            Assert.AreEqual("Unavailable", node.Description);
        }

        [TestMethod]
        public void ParseState_ReturnsExpectedState()
        {
            // Arrange
            var testCases = new (string Input, string Expected)[]
            {
                ("Ready", "Ready"),
                ("creating", "Creating"),
                ("UNAVAILABLE", "Unavailable"),
                ("", "Unknown"),
                (null, "Unknown"),
                ("invalid", "Unknown")
            };

            // Act & Assert
            foreach ((string input, string expected) in testCases)
            {
                var actual = SqlServerNode.ParseState(input);
                Assert.AreEqual(expected, actual.ToString());
            }
        }

        [TestMethod]
        public void State_Set_UpdatesDescriptionBasedOnState()
        {
            // Arrange
            var node = new SqlServerNode("sql1", "sub-123", "rg1", "Ready", "sql1.database.windows.net");

            // Act
            node.State = SqlServerState.Unavailable;

            // Assert
            Assert.AreEqual("Unavailable", node.Description);

            // Act
            node.State = SqlServerState.Ready;

            // Assert
            Assert.AreEqual("sql1.database.windows.net", node.Description);
        }
    }
}
