using AzureExplorer.FunctionApp.Models;

namespace AzureExplorer.Test.FunctionApp.Models
{
    [TestClass]
    public sealed class FunctionAppNodeTests
    {
        private const string _testName = "my-func";
        private const string _testSubscriptionId = "sub-123";
        private const string _testResourceGroupName = "rg-test";

        [TestMethod]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var node = new FunctionAppNode(_testName, _testSubscriptionId, _testResourceGroupName, "Running", "my-func.azurewebsites.net");

            // Assert
            Assert.AreEqual(_testName, node.Label);
            Assert.AreEqual(_testSubscriptionId, node.SubscriptionId);
            Assert.AreEqual(_testResourceGroupName, node.ResourceGroupName);
            Assert.AreEqual("my-func.azurewebsites.net", node.DefaultHostName);
            Assert.AreEqual("https://my-func.azurewebsites.net", node.BrowseUrl);
            Assert.AreEqual(PackageIds.FunctionAppContextMenu, node.ContextMenuId);
            Assert.IsTrue(node.SupportsChildren);
            Assert.HasCount(1, node.Children);
        }

        [TestMethod]
        [DataRow("functionapp", true)]
        [DataRow("functionapp,linux", true)]
        [DataRow("api,functionapp", true)]
        [DataRow("app", false)]
        [DataRow("", false)]
        [DataRow(null, false)]
        public void IsFunctionApp_ReturnsExpectedResult(string kind, bool expected)
        {
            // Act
            var actual = FunctionAppNode.IsFunctionApp(kind);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task LoadChildrenAsync_WithoutTags_AddsFilesNodeOnlyAsync()
        {
            // Arrange
            var node = new FunctionAppNode(_testName, _testSubscriptionId, _testResourceGroupName, "Running", "my-func.azurewebsites.net");

            // Act
            await node.LoadChildrenAsync();

            // Assert
            Assert.DoesNotContain(c => c.GetType().Name == "LoadingNode", node.Children);
            Assert.Contains(c => c.GetType().Name == "FilesNode", node.Children);
            Assert.DoesNotContain(c => c.GetType().Name == "TagsNode", node.Children);
        }

        [TestMethod]
        public async Task LoadChildrenAsync_WithTags_AddsFilesNodeAndTagsNodeAsync()
        {
            // Arrange
            var tags = new Dictionary<string, string>
            {
                ["env"] = "dev"
            };
            var node = new FunctionAppNode(_testName, _testSubscriptionId, _testResourceGroupName, "Running", "my-func.azurewebsites.net", tags);

            // Act
            await node.LoadChildrenAsync();

            // Assert
            Assert.Contains(c => c.GetType().Name == "FilesNode", node.Children);
            Assert.Contains(c => c.GetType().Name == "TagsNode", node.Children);
        }
    }
}
