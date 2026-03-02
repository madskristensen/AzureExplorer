using AzureExplorer.Core.Models;
using AzureExplorer.FunctionApp.Models;

namespace AzureExplorer.Test.FunctionApp.Models
{
    [TestClass]
    public sealed class SubscriptionFunctionAppsNodeTests
    {
        private const string _testSubscriptionId = "sub-123";

        [TestMethod]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var node = new SubscriptionFunctionAppsNode(_testSubscriptionId);

            // Assert
            Assert.AreEqual("Function Apps", node.Label);
            Assert.AreEqual(_testSubscriptionId, node.SubscriptionId);
            Assert.AreEqual(PackageIds.FunctionAppsCategoryContextMenu, node.ContextMenuId);
            Assert.IsTrue(node.SupportsChildren);
            Assert.HasCount(1, node.Children);
            Assert.IsInstanceOfType(node.Children[0], typeof(LoadingNode));
        }
    }
}
