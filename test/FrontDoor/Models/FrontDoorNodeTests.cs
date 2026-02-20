using AzureExplorer.FrontDoor.Models;

namespace AzureExplorer.Test.FrontDoor.Models
{
    [TestClass]
    public sealed class FrontDoorNodeTests
    {
        private const string _testName = "my-frontdoor";
        private const string _testSubscriptionId = "sub-123";
        private const string _testResourceGroupName = "rg-test";
        private const string _testHostName = "my-frontdoor.azurefd.net";

        [TestMethod]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var node = new FrontDoorNode(_testName, _testSubscriptionId, _testResourceGroupName, "Active", _testHostName);

            // Assert
            Assert.AreEqual(_testName, node.Label);
            Assert.AreEqual(_testSubscriptionId, node.SubscriptionId);
            Assert.AreEqual(_testResourceGroupName, node.ResourceGroupName);
            Assert.AreEqual(_testHostName, node.HostName);
            Assert.AreEqual($"https://{_testHostName}", node.BrowseUrl);
            Assert.AreEqual(FrontDoorState.Active, node.State);
            // Description is only set for non-normal states (Disabled)
            Assert.IsNull(node.Description);
        }

        [TestMethod]
        public void Constructor_WithNullOrEmptyHostName_SetsBrowseUrlToNull()
        {
            // Arrange & Act
            var nodeNull = new FrontDoorNode(_testName, _testSubscriptionId, _testResourceGroupName, "Active", hostName: null);
            var nodeEmpty = new FrontDoorNode(_testName, _testSubscriptionId, _testResourceGroupName, "Active", hostName: "");

            // Assert
            Assert.IsNull(nodeNull.BrowseUrl);
            Assert.IsNull(nodeEmpty.BrowseUrl);
        }

        [TestMethod]
        public void Constructor_ParsesStateCorrectly()
        {
            // Arrange & Act
            var activeNode = new FrontDoorNode(_testName, _testSubscriptionId, _testResourceGroupName, "Active", _testHostName);
            var disabledNode = new FrontDoorNode(_testName, _testSubscriptionId, _testResourceGroupName, "Disabled", _testHostName);
            var unknownNode = new FrontDoorNode(_testName, _testSubscriptionId, _testResourceGroupName, "InvalidState", _testHostName);
            var nullNode = new FrontDoorNode(_testName, _testSubscriptionId, _testResourceGroupName, state: null, _testHostName);

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
            var node = new FrontDoorNode(_testName, _testSubscriptionId, _testResourceGroupName, "Active", _testHostName);
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
