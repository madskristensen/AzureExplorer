using AzureExplorer.Core.Models;
using AzureExplorer.ResourceGroup.Models;

namespace AzureExplorer.Test.ResourceGroup.Models
{
    [TestClass]
    public sealed class ResourceGroupNodeTests
    {
        [TestMethod]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var node = new ResourceGroupNode("my-resource-group", "sub-123");

            // Assert
            Assert.AreEqual("my-resource-group", node.Label);
            Assert.AreEqual("sub-123", node.SubscriptionId);
            Assert.AreEqual("my-resource-group", node.ResourceGroupName);
            Assert.HasCount(1, node.Children);
            Assert.IsInstanceOfType(node.Children[0], typeof(LoadingNode));
        }
    }
}
