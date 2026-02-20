using AzureExplorer.Core.Search;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Test.Core.Search
{
    [TestClass]
    public sealed class SearchSubscriptionNodeTests
    {
        private static readonly ImageMoniker _testMoniker = new() { Guid = Guid.Empty, Id = 1 };

        [TestMethod]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var node = new SearchSubscriptionNode("sub123", "My Subscription");

            // Assert
            Assert.AreEqual("My Subscription", node.Label);
            Assert.AreEqual("sub123", node.SubscriptionId);
            Assert.IsTrue(node.IsExpanded, "Subscription nodes should be auto-expanded during search");
            Assert.IsTrue(node.IsLoaded, "Subscription nodes should be marked as loaded");
            Assert.IsTrue(node.SupportsChildren);
        }

        [TestMethod]
        public void AddResult_AddsNodeToChildren()
        {
            // Arrange
            var subNode = new SearchSubscriptionNode("sub123", "My Subscription");
            var resultNode = new SearchResultNode(
                "myresource",
                "Storage Account",
                "resourceId",
                "sub123",
                "My Subscription",
                "user@example.com",
                _testMoniker,
                actualNode: null);

            // Act
            subNode.AddResult(resultNode);

            // Assert
            Assert.HasCount(1, subNode.Children);
            Assert.AreSame(resultNode, subNode.Children[0]);
        }

        [TestMethod]
        public void AddResult_MultipleResults_AddsAllToChildren()
        {
            // Arrange
            var subNode = new SearchSubscriptionNode("sub123", "My Subscription");
            SearchResultNode result1 = CreateTestResultNode("resource1");
            SearchResultNode result2 = CreateTestResultNode("resource2");
            SearchResultNode result3 = CreateTestResultNode("resource3");

            // Act
            subNode.AddResult(result1);
            subNode.AddResult(result2);
            subNode.AddResult(result3);

            // Assert
            Assert.HasCount(3, subNode.Children);
        }

        [TestMethod]
        public void ContextMenuId_ReturnsZero()
        {
            // Arrange
            var node = new SearchSubscriptionNode("sub123", "My Subscription");

            // Act & Assert
            Assert.AreEqual(0, node.ContextMenuId, "Search subscription nodes should not have context menus");
        }

        #region Test Helpers

        private static SearchResultNode CreateTestResultNode(string resourceName)
        {
            return new SearchResultNode(
                resourceName,
                "Storage Account",
                $"resourceId/{resourceName}",
                "sub123",
                "My Subscription",
                "user@example.com",
                _testMoniker,
                actualNode: null);
        }

        #endregion
    }
}
