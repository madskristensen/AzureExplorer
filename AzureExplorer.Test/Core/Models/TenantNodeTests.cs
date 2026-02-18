using AzureExplorer.Core.Models;

namespace AzureExplorer.Test.Core.Models
{
    [TestClass]
    public sealed class TenantNodeTests
    {
        [TestMethod]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var node = new TenantNode("tenant-123", "My Tenant", "account-456");

            // Assert
            Assert.AreEqual("My Tenant", node.Label);
            Assert.AreEqual("tenant-123", node.TenantId);
            Assert.AreEqual("My Tenant", node.DisplayName);
            Assert.AreEqual("account-456", node.AccountId);
            Assert.HasCount(1, node.Children);
            Assert.IsInstanceOfType(node.Children[0], typeof(LoadingNode));
        }

        [TestMethod]
        public void Constructor_WithNullDisplayName_UsesLabelFromTenantId()
        {
            // Arrange & Act
            var node = new TenantNode("tenant-123", displayName: null, "account-456");

            // Assert
            Assert.AreEqual("tenant-123", node.Label);
        }
    }
}
