using AzureExplorer.Core.Models;

using Microsoft.VisualStudio.Imaging;

namespace AzureExplorer.Test.Core.Models
{
    [TestClass]
    public sealed class SubscriptionNodeTests
    {
        private const string _testSubscriptionId = "sub-123";
        private const string _testName = "Test Subscription";

        #region Constructor Tests

        [TestMethod]
        public void Constructor_SetsName()
        {
            // Arrange & Act
            var node = new SubscriptionNode(_testName, _testSubscriptionId);

            // Assert
            Assert.AreEqual(_testName, node.Label);
        }

        [TestMethod]
        public void Constructor_SetsSubscriptionId()
        {
            // Arrange & Act
            var node = new SubscriptionNode(_testName, _testSubscriptionId);

            // Assert
            Assert.AreEqual(_testSubscriptionId, node.SubscriptionId);
        }

        [TestMethod]
        public void Constructor_AddsLoadingNode()
        {
            // Arrange & Act
            var node = new SubscriptionNode(_testName, _testSubscriptionId);

            // Assert
            Assert.HasCount(1, node.Children);
            Assert.IsInstanceOfType(node.Children[0], typeof(LoadingNode));
        }

        #endregion

        #region Property Tests

        [TestMethod]
        public void ContextMenuId_ReturnsSubscriptionContextMenu()
        {
            // Arrange
            var node = new SubscriptionNode(_testName, _testSubscriptionId);

            // Act
            var contextMenuId = node.ContextMenuId;

            // Assert
            Assert.AreEqual(PackageIds.SubscriptionContextMenu, contextMenuId);
        }

        [TestMethod]
        public void SupportsChildren_ReturnsTrue()
        {
            // Arrange
            var node = new SubscriptionNode(_testName, _testSubscriptionId);

            // Act
            var supportsChildren = node.SupportsChildren;

            // Assert
            Assert.IsTrue(supportsChildren);
        }

        #endregion

        #region LoadChildrenAsync Tests

        [TestMethod]
        public async Task LoadChildrenAsync_AddsSubscriptionAppServicesNodeAsync()
        {
            // Arrange
            var node = new SubscriptionNode(_testName, _testSubscriptionId);

            // Act
            await node.LoadChildrenAsync();

            // Assert
            Assert.IsTrue(node.Children.Any(c => c.GetType().Name == "SubscriptionAppServicesNode"));
        }

        [TestMethod]
        public async Task LoadChildrenAsync_AddsSubscriptionFrontDoorsNodeAsync()
        {
            // Arrange
            var node = new SubscriptionNode(_testName, _testSubscriptionId);

            // Act
            await node.LoadChildrenAsync();

            // Assert
            Assert.IsTrue(node.Children.Any(c => c.GetType().Name == "SubscriptionFrontDoorsNode"));
        }

        [TestMethod]
        public async Task LoadChildrenAsync_AddsSubscriptionKeyVaultsNodeAsync()
        {
            // Arrange
            var node = new SubscriptionNode(_testName, _testSubscriptionId);

            // Act
            await node.LoadChildrenAsync();

            // Assert
            Assert.IsTrue(node.Children.Any(c => c.GetType().Name == "SubscriptionKeyVaultsNode"));
        }

        [TestMethod]
        public async Task LoadChildrenAsync_AddsResourceGroupsNodeAsync()
        {
            // Arrange
            var node = new SubscriptionNode(_testName, _testSubscriptionId);

            // Act
            await node.LoadChildrenAsync();

            // Assert
            Assert.IsTrue(node.Children.Any(c => c.GetType().Name == "ResourceGroupsNode"));
        }

        [TestMethod]
        public async Task LoadChildrenAsync_AddsFourChildNodesAsync()
        {
            // Arrange
            var node = new SubscriptionNode(_testName, _testSubscriptionId);

            // Act
            await node.LoadChildrenAsync();

            // Assert
            Assert.HasCount(4, node.Children);
        }

        [TestMethod]
        public async Task LoadChildrenAsync_RemovesLoadingNodeAsync()
        {
            // Arrange
            var node = new SubscriptionNode(_testName, _testSubscriptionId);

            // Act
            await node.LoadChildrenAsync();

            // Assert
            Assert.IsFalse(node.Children.Any(c => c is LoadingNode));
        }

        [TestMethod]
        public async Task LoadChildrenAsync_SetsIsLoadedAsync()
        {
            // Arrange
            var node = new SubscriptionNode(_testName, _testSubscriptionId);

            // Act
            await node.LoadChildrenAsync();

            // Assert
            Assert.IsTrue(node.IsLoaded);
        }

        [TestMethod]
        public async Task LoadChildrenAsync_SetsIsLoadingFalseAsync()
        {
            // Arrange
            var node = new SubscriptionNode(_testName, _testSubscriptionId);

            // Act
            await node.LoadChildrenAsync();

            // Assert
            Assert.IsFalse(node.IsLoading);
        }

        [TestMethod]
        public async Task LoadChildrenAsync_WhenAlreadyLoaded_ReturnsWithoutLoadingAsync()
        {
            // Arrange
            var node = new SubscriptionNode(_testName, _testSubscriptionId);

            // Mark as already loaded by accessing internal state
            typeof(ExplorerNodeBase)
                .GetProperty("IsLoaded")!
                .SetValue(node, true);

            var initialChildCount = node.Children.Count;

            // Act
            await node.LoadChildrenAsync();

            // Assert - Should not change children
            Assert.HasCount(initialChildCount, node.Children);
        }

        [TestMethod]
        public async Task LoadChildrenAsync_WhenAlreadyLoading_ReturnsWithoutLoadingAsync()
        {
            // Arrange
            var node = new SubscriptionNode(_testName, _testSubscriptionId);

            // Mark as already loading
            typeof(ExplorerNodeBase)
                .GetProperty("IsLoading")!
                .SetValue(node, true);

            var initialChildCount = node.Children.Count;

            // Act
            await node.LoadChildrenAsync();

            // Assert - Should not change children
            Assert.HasCount(initialChildCount, node.Children);
        }

        [TestMethod]
        public async Task LoadChildrenAsync_WithCancellationToken_RespectsCancellationAsync()
        {
            // Arrange
            var node = new SubscriptionNode(_testName, _testSubscriptionId);
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            await node.LoadChildrenAsync(cts.Token);

            // Assert - Should complete without exception even with cancelled token
            // The method passes token to PreloadChildrenAsync which handles cancellation gracefully
            Assert.IsTrue(node.IsLoaded);
        }

        [TestMethod]
        public async Task LoadChildrenAsync_SetsParentOnChildNodesAsync()
        {
            // Arrange
            var node = new SubscriptionNode(_testName, _testSubscriptionId);

            // Act
            await node.LoadChildrenAsync();

            // Assert
            Assert.IsTrue(node.Children.All(c => c.Parent == node));
        }

        [TestMethod]
        public async Task LoadChildrenAsync_ChildNodesInCorrectOrderAsync()
        {
            // Arrange
            var node = new SubscriptionNode(_testName, _testSubscriptionId);

            // Act
            await node.LoadChildrenAsync();

            // Assert
            Assert.AreEqual("SubscriptionAppServicesNode", node.Children[0].GetType().Name);
            Assert.AreEqual("SubscriptionFrontDoorsNode", node.Children[1].GetType().Name);
            Assert.AreEqual("SubscriptionKeyVaultsNode", node.Children[2].GetType().Name);
            Assert.AreEqual("ResourceGroupsNode", node.Children[3].GetType().Name);
        }

        #endregion
    }
}

