using AzureExplorer.Core.Models;

namespace AzureExplorer.Test.Core.Models
{
    [TestClass]
    public sealed class AccountNodeTests
    {
        [TestMethod]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var node = new AccountNode("account-123", "Test Account");

            // Assert
            Assert.AreEqual("account-123", node.AccountId);
            Assert.AreEqual("Test Account", node.Label);
            Assert.AreEqual(1, node.Children.Count);
            Assert.IsInstanceOfType<LoadingNode>(node.Children[0]);
        }
    }
}
