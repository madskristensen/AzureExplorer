using AzureExplorer.Core.Models;
using AzureExplorer.KeyVault.Models;

namespace AzureExplorer.Test.KeyVault.Models
{
    [TestClass]
    public sealed class SubscriptionKeyVaultsNodeTests
    {
        [TestMethod]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var node = new SubscriptionKeyVaultsNode("sub-123");

            // Assert
            Assert.AreEqual("Key Vaults", node.Label);
            Assert.AreEqual("sub-123", node.SubscriptionId);
            Assert.HasCount(1, node.Children);
            Assert.IsInstanceOfType(node.Children[0], typeof(LoadingNode));
        }
    }
}
