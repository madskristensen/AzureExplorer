using AzureExplorer.Core.Search;

namespace AzureExplorer.Test.Core.Search
{
    [TestClass]
    public sealed class SearchAccountNodeTests
    {
        [TestMethod]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var node = new SearchAccountNode("account123", "user@example.com");

            // Assert
            Assert.AreEqual("user@example.com", node.Label);
            Assert.AreEqual("account123", node.AccountId);
            Assert.IsTrue(node.IsExpanded, "Account nodes should be auto-expanded during search");
            Assert.IsTrue(node.IsLoaded, "Account nodes should be marked as loaded");
            Assert.IsTrue(node.SupportsChildren);
        }

        [TestMethod]
        public void GetOrCreateSubscription_NewSubscription_CreatesAndAddsNode()
        {
            // Arrange
            var node = new SearchAccountNode("account123", "user@example.com");

            // Act
            SearchSubscriptionNode subNode = node.GetOrCreateSubscription("sub1", "My Subscription");

            // Assert
            Assert.IsNotNull(subNode);
            Assert.AreEqual("sub1", subNode.SubscriptionId);
            Assert.AreEqual("My Subscription", subNode.Label);
            Assert.HasCount(1, node.Children);
            Assert.AreSame(subNode, node.Children[0]);
        }

        [TestMethod]
        public void GetOrCreateSubscription_ExistingSubscription_ReturnsSameNode()
        {
            // Arrange
            var node = new SearchAccountNode("account123", "user@example.com");
            SearchSubscriptionNode firstCall = node.GetOrCreateSubscription("sub1", "My Subscription");

            // Act
            SearchSubscriptionNode secondCall = node.GetOrCreateSubscription("sub1", "My Subscription");

            // Assert
            Assert.AreSame(firstCall, secondCall);
            Assert.HasCount(1, node.Children, "Should not add duplicate subscription");
        }

        [TestMethod]
        public void GetOrCreateSubscription_MultipleSubscriptions_CreatesMultipleNodes()
        {
            // Arrange
            var node = new SearchAccountNode("account123", "user@example.com");

            // Act
            SearchSubscriptionNode sub1 = node.GetOrCreateSubscription("sub1", "Subscription 1");
            SearchSubscriptionNode sub2 = node.GetOrCreateSubscription("sub2", "Subscription 2");
            SearchSubscriptionNode sub3 = node.GetOrCreateSubscription("sub3", "Subscription 3");

            // Assert
            Assert.HasCount(3, node.Children);
            Assert.AreNotSame(sub1, sub2);
            Assert.AreNotSame(sub2, sub3);
        }

        [TestMethod]
        public void ContextMenuId_ReturnsZero()
        {
            // Arrange
            var node = new SearchAccountNode("account123", "user@example.com");

            // Act & Assert
            Assert.AreEqual(0, node.ContextMenuId, "Search account nodes should not have context menus");
        }
    }
}
