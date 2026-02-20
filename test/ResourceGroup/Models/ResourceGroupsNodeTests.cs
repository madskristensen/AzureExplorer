using AzureExplorer.Core.Models;
using AzureExplorer.ResourceGroup.Models;

namespace AzureExplorer.Test.ResourceGroup.Models
{
    [TestClass]
    public sealed class ResourceGroupsNodeTests
    {
        [TestMethod]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var node = new ResourceGroupsNode("sub-123");

            // Assert
            Assert.AreEqual("Resource Groups", node.Label);
            Assert.AreEqual("sub-123", node.SubscriptionId);
            Assert.HasCount(1, node.Children);
            Assert.IsInstanceOfType(node.Children[0], typeof(LoadingNode));
        }
    }
}
