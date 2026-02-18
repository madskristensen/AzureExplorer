using AzureExplorer.AppServicePlan.Models;
using AzureExplorer.Core.Models;

namespace AzureExplorer.Test.AppServicePlan.Models
{
    [TestClass]
    public sealed class AppServicePlansNodeTests
    {
        [TestMethod]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var node = new AppServicePlansNode("sub-123", "rg-test");

            // Assert
            Assert.AreEqual("sub-123", node.SubscriptionId);
            Assert.AreEqual("rg-test", node.ResourceGroupName);
            Assert.AreEqual("App Service Plans", node.Label);
            Assert.HasCount(1, node.Children);
            Assert.IsInstanceOfType(node.Children[0], typeof(LoadingNode));
        }
    }
}
