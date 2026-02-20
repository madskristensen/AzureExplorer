using AzureExplorer.Core.Models;
using AzureExplorer.FrontDoor.Models;

namespace AzureExplorer.Test.FrontDoor.Models
{
    [TestClass]
    public sealed class FrontDoorsNodeTests
    {
        [TestMethod]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var node = new FrontDoorsNode("test-subscription-id", "test-rg");

            // Assert
            Assert.AreEqual("test-subscription-id", node.SubscriptionId);
            Assert.AreEqual("test-rg", node.ResourceGroupName);
            Assert.AreEqual("Front Doors", node.Label);
            Assert.HasCount(1, node.Children);
            Assert.IsInstanceOfType(node.Children[0], typeof(LoadingNode));
        }
    }
}
