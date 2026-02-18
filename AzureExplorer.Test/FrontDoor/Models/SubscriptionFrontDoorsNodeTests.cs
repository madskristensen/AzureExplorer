using AzureExplorer.Core.Models;
using AzureExplorer.FrontDoor.Models;

namespace AzureExplorer.Test.FrontDoor.Models
{
    [TestClass]
    public sealed class SubscriptionFrontDoorsNodeTests
    {
        [TestMethod]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var node = new SubscriptionFrontDoorsNode("sub-123");

            // Assert
            Assert.AreEqual("Front Doors", node.Label);
            Assert.AreEqual("sub-123", node.SubscriptionId);
            Assert.HasCount(1, node.Children);
            Assert.IsInstanceOfType(node.Children[0], typeof(LoadingNode));
        }
    }
}
