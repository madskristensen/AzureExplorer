using AzureExplorer.Core.Models;
using AzureExplorer.KeyVault.Models;

namespace AzureExplorer.Test.KeyVault.Models
{
    [TestClass]
    public sealed class KeyVaultsNodeTests
    {
        private const string TestSubscriptionId = "sub-123";
        private const string TestResourceGroupName = "rg-test";

        [TestMethod]
        public void Constructor_SetsSubscriptionId()
        {
            // Arrange & Act
            var node = new KeyVaultsNode(TestSubscriptionId, TestResourceGroupName);

            // Assert
            Assert.AreEqual(TestSubscriptionId, node.SubscriptionId);
        }

        [TestMethod]
        public void Constructor_SetsResourceGroupName()
        {
            // Arrange & Act
            var node = new KeyVaultsNode(TestSubscriptionId, TestResourceGroupName);

            // Assert
            Assert.AreEqual(TestResourceGroupName, node.ResourceGroupName);
        }

        [TestMethod]
        public void Constructor_SetsLabelAndAddsLoadingNode()
        {
            // Arrange & Act
            var node = new KeyVaultsNode(TestSubscriptionId, TestResourceGroupName);

            // Assert
            Assert.AreEqual("Key Vaults", node.Label);
            Assert.HasCount(1, node.Children);
            Assert.IsInstanceOfType(node.Children[0], typeof(LoadingNode));
        }
    }
}
