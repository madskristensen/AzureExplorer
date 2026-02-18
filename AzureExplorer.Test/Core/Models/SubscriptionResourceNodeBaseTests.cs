using AzureExplorer.Core.Models;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using AzureExplorer.Core.Services;
using Microsoft.VisualStudio.Imaging.Interop;
using Moq;

namespace AzureExplorer.Test.Core.Models
{
    [TestClass]
    public sealed class SubscriptionResourceNodeBaseTests
    {
        private const string _testLabel = "Test Resources";
        private const string _testSubscriptionId = "sub-12345";

        #region Constructor Tests

        [TestMethod]
        public void Constructor_SetsLabel()
        {
            // Arrange & Act
            var node = new TestSubscriptionResourceNode(_testLabel, _testSubscriptionId);

            // Assert
            Assert.AreEqual(_testLabel, node.Label);
        }

        [TestMethod]
        public void Constructor_SetsSubscriptionId()
        {
            // Arrange & Act
            var node = new TestSubscriptionResourceNode(_testLabel, _testSubscriptionId);

            // Assert
            Assert.AreEqual(_testSubscriptionId, node.SubscriptionId);
        }

        [TestMethod]
        public void Constructor_AddsLoadingNode()
        {
            // Arrange & Act
            var node = new TestSubscriptionResourceNode(_testLabel, _testSubscriptionId);

            // Assert
            Assert.HasCount(1, node.Children);
            Assert.IsInstanceOfType(node.Children[0], typeof(LoadingNode));
        }

        #endregion

        #region SupportsChildren Tests

        [TestMethod]
        public void SupportsChildren_ReturnsTrue()
        {
            // Arrange
            var node = new TestSubscriptionResourceNode(_testLabel, _testSubscriptionId);

            // Act
            var supportsChildren = node.SupportsChildren;

            // Assert
            Assert.IsTrue(supportsChildren);
        }

        #endregion

        #region InsertChildSorted Tests

        [TestMethod]
        public void InsertChildSorted_InsertsFirstChild()
        {
            // Arrange
            var node = new TestSubscriptionResourceNode(_testLabel, _testSubscriptionId);
            node.Children.Clear();
            var child = new TestResourceNode("Child1");

            // Act
            node.InsertChildSortedPublic(child);

            // Assert
            Assert.HasCount(1, node.Children);
            Assert.AreEqual("Child1", node.Children[0].Label);
            Assert.AreEqual(node, child.Parent);
        }

        [TestMethod]
        public void InsertChildSorted_InsertsChildrenInAlphabeticalOrder()
        {
            // Arrange
            var node = new TestSubscriptionResourceNode(_testLabel, _testSubscriptionId);
            node.Children.Clear();
            var child1 = new TestResourceNode("Charlie");
            var child2 = new TestResourceNode("Alice");
            var child3 = new TestResourceNode("Bob");

            // Act
            node.InsertChildSortedPublic(child1);
            node.InsertChildSortedPublic(child2);
            node.InsertChildSortedPublic(child3);

            // Assert
            Assert.HasCount(3, node.Children);
            Assert.AreEqual("Alice", node.Children[0].Label);
            Assert.AreEqual("Bob", node.Children[1].Label);
            Assert.AreEqual("Charlie", node.Children[2].Label);
        }

        [TestMethod]
        public void InsertChildSorted_IsCaseInsensitive()
        {
            // Arrange
            var node = new TestSubscriptionResourceNode(_testLabel, _testSubscriptionId);
            node.Children.Clear();
            var child1 = new TestResourceNode("apple");
            var child2 = new TestResourceNode("Banana");
            var child3 = new TestResourceNode("CHERRY");

            // Act
            node.InsertChildSortedPublic(child1);
            node.InsertChildSortedPublic(child2);
            node.InsertChildSortedPublic(child3);

            // Assert
            Assert.HasCount(3, node.Children);
            Assert.AreEqual("apple", node.Children[0].Label);
            Assert.AreEqual("Banana", node.Children[1].Label);
            Assert.AreEqual("CHERRY", node.Children[2].Label);
        }

        [TestMethod]
        public void InsertChildSorted_SetsParentReference()
        {
            // Arrange
            var node = new TestSubscriptionResourceNode(_testLabel, _testSubscriptionId);
            node.Children.Clear();
            var child = new TestResourceNode("TestChild");

            // Act
            node.InsertChildSortedPublic(child);

            // Assert
            Assert.AreEqual(node, child.Parent);
        }

        [TestMethod]
        public void InsertChildSorted_InsertsAtBeginning()
        {
            // Arrange
            var node = new TestSubscriptionResourceNode(_testLabel, _testSubscriptionId);
            node.Children.Clear();
            node.Children.Add(new TestResourceNode("Bob"));
            node.Children.Add(new TestResourceNode("Charlie"));
            var child = new TestResourceNode("Alice");

            // Act
            node.InsertChildSortedPublic(child);

            // Assert
            Assert.HasCount(3, node.Children);
            Assert.AreEqual("Alice", node.Children[0].Label);
        }

        [TestMethod]
        public void InsertChildSorted_InsertsAtEnd()
        {
            // Arrange
            var node = new TestSubscriptionResourceNode(_testLabel, _testSubscriptionId);
            node.Children.Clear();
            node.Children.Add(new TestResourceNode("Alice"));
            node.Children.Add(new TestResourceNode("Bob"));
            var child = new TestResourceNode("Charlie");

            // Act
            node.InsertChildSortedPublic(child);

            // Assert
            Assert.HasCount(3, node.Children);
            Assert.AreEqual("Charlie", node.Children[2].Label);
        }

        [TestMethod]
        public void InsertChildSorted_InsertsInMiddle()
        {
            // Arrange
            var node = new TestSubscriptionResourceNode(_testLabel, _testSubscriptionId);
            node.Children.Clear();
            node.Children.Add(new TestResourceNode("Alice"));
            node.Children.Add(new TestResourceNode("Charlie"));
            var child = new TestResourceNode("Bob");

            // Act
            node.InsertChildSortedPublic(child);

            // Assert
            Assert.HasCount(3, node.Children);
            Assert.AreEqual("Bob", node.Children[1].Label);
        }

        [TestMethod]
        public void InsertChildSorted_HandlesIdenticalLabels()
        {
            // Arrange
            var node = new TestSubscriptionResourceNode(_testLabel, _testSubscriptionId);
            node.Children.Clear();
            var child1 = new TestResourceNode("Duplicate");
            var child2 = new TestResourceNode("Duplicate");

            // Act
            node.InsertChildSortedPublic(child1);
            node.InsertChildSortedPublic(child2);

            // Assert
            Assert.HasCount(2, node.Children);
            Assert.AreEqual("Duplicate", node.Children[0].Label);
            Assert.AreEqual("Duplicate", node.Children[1].Label);
        }

        #endregion

        #region Helper Classes

        private sealed class TestSubscriptionResourceNode : SubscriptionResourceNodeBase
        {
            public TestSubscriptionResourceNode(string label, string subscriptionId)
                : base(label, subscriptionId)
            {
            }

            public override ImageMoniker IconMoniker => default;

            public override int ContextMenuId => 0;

            protected override string ResourceType => "Microsoft.Test/resources";

            protected override ExplorerNodeBase CreateNodeFromResource(string name, string resourceGroup, GenericResource resource)
            {
                return new TestResourceNode(name);
            }

            public void InsertChildSortedPublic(ExplorerNodeBase node)
            {
                InsertChildSorted(node);
            }
        }

        private sealed class TestResourceNode : ExplorerNodeBase
        {
            public TestResourceNode(string label) : base(label)
            {
            }

            public override ImageMoniker IconMoniker => default;

            public override int ContextMenuId => 0;

            public override bool SupportsChildren => false;
        }

        #endregion
    }
}
