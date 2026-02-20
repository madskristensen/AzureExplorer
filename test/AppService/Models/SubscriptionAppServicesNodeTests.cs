using AzureExplorer.AppService.Models;
using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging;

namespace AzureExplorer.Test.AppService.Models
{
    [TestClass]
    public sealed class SubscriptionAppServicesNodeTests
    {
        private const string _testSubscriptionId = "sub-123";

        #region Constructor Tests

        [TestMethod]
        public void Constructor_SetsSubscriptionId()
        {
            // Arrange & Act
            var node = new SubscriptionAppServicesNode(_testSubscriptionId);

            // Assert
            Assert.AreEqual(_testSubscriptionId, node.SubscriptionId);
        }

        [TestMethod]
        public void Constructor_SetsLabel()
        {
            // Arrange & Act
            var node = new SubscriptionAppServicesNode(_testSubscriptionId);

            // Assert
            Assert.AreEqual("App Services", node.Label);
        }

        [TestMethod]
        public void Constructor_AddsLoadingNode()
        {
            // Arrange & Act
            var node = new SubscriptionAppServicesNode(_testSubscriptionId);

            // Assert
            Assert.HasCount(1, node.Children);
            Assert.IsInstanceOfType(node.Children[0], typeof(LoadingNode));
        }

        [TestMethod]
        public void Constructor_WithNullSubscriptionId_SetsSubscriptionId()
        {
            // Arrange & Act
            var node = new SubscriptionAppServicesNode(null!);

            // Assert
            Assert.IsNull(node.SubscriptionId);
        }

        [TestMethod]
        public void Constructor_WithEmptySubscriptionId_SetsSubscriptionId()
        {
            // Arrange & Act
            var node = new SubscriptionAppServicesNode(string.Empty);

            // Assert
            Assert.AreEqual(string.Empty, node.SubscriptionId);
        }

        #endregion

        #region Property Tests

        [TestMethod]
        public void ContextMenuId_ReturnsAppServicesCategoryContextMenu()
        {
            // Arrange
            var node = new SubscriptionAppServicesNode(_testSubscriptionId);

            // Act
            var contextMenuId = node.ContextMenuId;

            // Assert
            Assert.AreEqual(PackageIds.AppServicesCategoryContextMenu, contextMenuId);
        }

        #endregion
    }
}
