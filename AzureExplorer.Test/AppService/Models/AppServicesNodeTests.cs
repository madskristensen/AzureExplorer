using AzureExplorer.AppService.Models;
using AzureExplorer.Core.Models;

namespace AzureExplorer.Test.AppService.Models
{
    [TestClass]
    public sealed class AppServicesNodeTests
    {
        [TestMethod]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var node = new AppServicesNode("sub-123", "rg-test");

            // Assert
            Assert.AreEqual("sub-123", node.SubscriptionId);
            Assert.AreEqual("rg-test", node.ResourceGroupName);
            Assert.AreEqual("App Services", node.Label);
            Assert.HasCount(1, node.Children);
            Assert.IsInstanceOfType(node.Children[0], typeof(LoadingNode));
        }
    }
}
