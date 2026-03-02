using AzureExplorer.Storage.Models;
using AzureExplorer.Core.Models;

namespace AzureExplorer.Test.Storage.Models
{
    [TestClass]
    public sealed class StorageAccountNodeTests
    {
        [TestMethod]
        public void Constructor_WithSucceededState_SetsFormattedDescription()
        {
            // Arrange & Act
            var node = new StorageAccountNode("stacc1", "sub-123", "rg1", "Succeeded", "StorageV2", "Standard_LRS");

            // Assert
            Assert.AreEqual(ProvisioningState.Succeeded, node.State);
            Assert.AreEqual("StorageV2 (Standard_LRS)", node.Description);
            Assert.AreEqual(PackageIds.StorageAccountContextMenu, node.ContextMenuId);
            Assert.IsTrue(node.SupportsChildren);
            Assert.HasCount(1, node.Children);
        }

        [TestMethod]
        public void Constructor_WithFailedState_SetsFailedDescription()
        {
            // Arrange & Act
            var node = new StorageAccountNode("stacc1", "sub-123", "rg1", "Failed", "StorageV2", "Standard_LRS");

            // Assert
            Assert.AreEqual(ProvisioningState.Failed, node.State);
            Assert.AreEqual("Failed", node.Description);
        }

        [TestMethod]
        public void State_Set_UpdatesDescriptionBasedOnState()
        {
            // Arrange
            var node = new StorageAccountNode("stacc1", "sub-123", "rg1", "Succeeded", "StorageV2", "Standard_LRS");

            // Act
            node.State = ProvisioningState.Failed;

            // Assert
            Assert.AreEqual("Failed", node.Description);

            // Act
            node.State = ProvisioningState.Succeeded;

            // Assert
            Assert.AreEqual("StorageV2 (Standard_LRS)", node.Description);
        }
    }
}
