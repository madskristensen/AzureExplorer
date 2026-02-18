using AzureExplorer.FrontDoor.Models;

namespace AzureExplorer.Test.FrontDoor.Models
{
    [TestClass]
    public sealed class FrontDoorNodeTests
    {
        private const string TestName = "my-frontdoor";
        private const string TestSubscriptionId = "sub-123";
        private const string TestResourceGroupName = "rg-test";
        private const string TestHostName = "my-frontdoor.azurefd.net";

        [TestMethod]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var node = new FrontDoorNode(TestName, TestSubscriptionId, TestResourceGroupName, "Active", TestHostName);

            // Assert
            Assert.AreEqual(TestName, node.Label);
            Assert.AreEqual(TestSubscriptionId, node.SubscriptionId);
            Assert.AreEqual(TestResourceGroupName, node.ResourceGroupName);
            Assert.AreEqual(TestHostName, node.HostName);
            Assert.AreEqual($"https://{TestHostName}", node.BrowseUrl);
            Assert.AreEqual(FrontDoorState.Active, node.State);
            Assert.AreEqual("Active", node.Description);
        }

        [TestMethod]
        public void Constructor_WithNullOrEmptyHostName_SetsBrowseUrlToNull()
        {
            // Arrange & Act
            var nodeNull = new FrontDoorNode(TestName, TestSubscriptionId, TestResourceGroupName, "Active", hostName: null);
            var nodeEmpty = new FrontDoorNode(TestName, TestSubscriptionId, TestResourceGroupName, "Active", hostName: "");

            // Assert
            Assert.IsNull(nodeNull.BrowseUrl);
            Assert.IsNull(nodeEmpty.BrowseUrl);
        }

        [TestMethod]
        public void Constructor_ParsesStateCorrectly()
        {
            // Arrange & Act
            var activeNode = new FrontDoorNode(TestName, TestSubscriptionId, TestResourceGroupName, "Active", TestHostName);
            var disabledNode = new FrontDoorNode(TestName, TestSubscriptionId, TestResourceGroupName, "Disabled", TestHostName);
            var unknownNode = new FrontDoorNode(TestName, TestSubscriptionId, TestResourceGroupName, "InvalidState", TestHostName);
            var nullNode = new FrontDoorNode(TestName, TestSubscriptionId, TestResourceGroupName, state: null, TestHostName);

            // Assert
            Assert.AreEqual(FrontDoorState.Active, activeNode.State);
            Assert.AreEqual(FrontDoorState.Disabled, disabledNode.State);
            Assert.AreEqual(FrontDoorState.Unknown, unknownNode.State);
            Assert.AreEqual(FrontDoorState.Unknown, nullNode.State);
        }

        [TestMethod]
        public void State_Set_UpdatesDescriptionAndRaisesIconMonikerChanged()
        {
            // Arrange
            var node = new FrontDoorNode(TestName, TestSubscriptionId, TestResourceGroupName, "Active", TestHostName);
            var iconMonikerChanged = false;

            node.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == "IconMoniker")
                    iconMonikerChanged = true;
            };

            // Act
            node.State = FrontDoorState.Disabled;

            // Assert
            Assert.AreEqual("Disabled", node.Description);
            Assert.IsTrue(iconMonikerChanged);
        }
    }
}
