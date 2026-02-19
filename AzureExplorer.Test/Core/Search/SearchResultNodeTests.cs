using AzureExplorer.Core.Models;
using AzureExplorer.Core.Search;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Test.Core.Search
{
    [TestClass]
    public sealed class SearchResultNodeTests
    {
        private static readonly ImageMoniker _testMoniker = new() { Guid = Guid.Empty, Id = 1 };

        [TestMethod]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var node = new SearchResultNode(
                "myresource",
                "Storage Account",
                "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.Storage/storageAccounts/myresource",
                "sub1",
                "My Subscription",
                "user@example.com",
                _testMoniker,
                actualNode: null);

            // Assert
            Assert.AreEqual("myresource (Storage Account)", node.Label);
            Assert.AreEqual("myresource", node.ResourceName);
            Assert.AreEqual("Storage Account", node.ResourceType);
            Assert.AreEqual("sub1", node.SubscriptionId);
            Assert.AreEqual("My Subscription", node.SubscriptionName);
            Assert.AreEqual("user@example.com", node.AccountName);
        }

        [TestMethod]
        public void SupportsChildren_WithNullActualNode_ReturnsFalse()
        {
            // Arrange
            var node = new SearchResultNode(
                "myresource",
                "Storage Account",
                "resourceId",
                "sub1",
                "My Subscription",
                "user@example.com",
                _testMoniker,
                actualNode: null);

            // Act & Assert
            Assert.IsFalse(node.SupportsChildren);
        }

        [TestMethod]
        public void SupportsChildren_WithActualNodeThatSupportsChildren_ReturnsTrue()
        {
            // Arrange
            var actualNode = new TestNodeWithChildren();
            var node = new SearchResultNode(
                "myresource",
                "Storage Account",
                "resourceId",
                "sub1",
                "My Subscription",
                "user@example.com",
                _testMoniker,
                actualNode);

            // Act & Assert
            Assert.IsTrue(node.SupportsChildren);
        }

        [TestMethod]
        public void SupportsChildren_WithActualNodeThatDoesNotSupportChildren_ReturnsFalse()
        {
            // Arrange
            var actualNode = new TestNodeWithoutChildren();
            var node = new SearchResultNode(
                "myresource",
                "Storage Account",
                "resourceId",
                "sub1",
                "My Subscription",
                "user@example.com",
                _testMoniker,
                actualNode);

            // Act & Assert
            Assert.IsFalse(node.SupportsChildren);
        }

        [TestMethod]
        public void ContextMenuId_WithNullActualNode_ReturnsZero()
        {
            // Arrange
            var node = new SearchResultNode(
                "myresource",
                "Storage Account",
                "resourceId",
                "sub1",
                "My Subscription",
                "user@example.com",
                _testMoniker,
                actualNode: null);

            // Act & Assert
            Assert.AreEqual(0, node.ContextMenuId);
        }

        [TestMethod]
        public void ContextMenuId_WithActualNode_DelegatesToActualNode()
        {
            // Arrange
            var actualNode = new TestNodeWithContextMenu(42);
            var node = new SearchResultNode(
                "myresource",
                "Storage Account",
                "resourceId",
                "sub1",
                "My Subscription",
                "user@example.com",
                _testMoniker,
                actualNode);

            // Act & Assert
            Assert.AreEqual(42, node.ContextMenuId);
        }

        [TestMethod]
        public void Constructor_WithActualNodeThatSupportsChildren_AddsLoadingPlaceholder()
        {
            // Arrange
            var actualNode = new TestNodeWithChildren();

            // Act
            var node = new SearchResultNode(
                "myresource",
                "Storage Account",
                "resourceId",
                "sub1",
                "My Subscription",
                "user@example.com",
                _testMoniker,
                actualNode);

            // Assert
            Assert.HasCount(1, node.Children);
            Assert.IsInstanceOfType(node.Children[0], typeof(LoadingNode));
        }

        [TestMethod]
        public void Constructor_WithNullActualNode_HasNoChildren()
        {
            // Arrange & Act
            var node = new SearchResultNode(
                "myresource",
                "Storage Account",
                "resourceId",
                "sub1",
                "My Subscription",
                "user@example.com",
                _testMoniker,
                actualNode: null);

            // Assert
            Assert.IsEmpty(node.Children);
        }

        #region Test Helpers

        private sealed class TestNodeWithChildren : ExplorerNodeBase
        {
            public TestNodeWithChildren() : base("Test Node") { }

            public override ImageMoniker IconMoniker => default;
            public override int ContextMenuId => 0;
            public override bool SupportsChildren => true;
        }

        private sealed class TestNodeWithoutChildren : ExplorerNodeBase
        {
            public TestNodeWithoutChildren() : base("Test Node") { }

            public override ImageMoniker IconMoniker => default;
            public override int ContextMenuId => 0;
            public override bool SupportsChildren => false;
        }

        private sealed class TestNodeWithContextMenu(int contextMenuId) : ExplorerNodeBase("Test Node")
        {
            public override ImageMoniker IconMoniker => default;
            public override int ContextMenuId => contextMenuId;
            public override bool SupportsChildren => false;
        }

        #endregion
    }
}
