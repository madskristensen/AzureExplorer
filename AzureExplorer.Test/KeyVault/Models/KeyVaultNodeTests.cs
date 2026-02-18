using AzureExplorer.Core.Models;
using AzureExplorer.KeyVault.Models;

namespace AzureExplorer.Test.KeyVault.Models
{
    [TestClass]
    public sealed class KeyVaultNodeTests
    {
        private const string TestName = "my-keyvault";
        private const string TestSubscriptionId = "sub-123";
        private const string TestResourceGroupName = "rg-test";
        private const string TestVaultUri = "https://my-keyvault.vault.azure.net/";

        #region Constructor Tests

        [TestMethod]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var node = new KeyVaultNode(TestName, TestSubscriptionId, TestResourceGroupName, "Succeeded", TestVaultUri);

            // Assert
            Assert.AreEqual(TestName, node.Label);
            Assert.AreEqual(TestSubscriptionId, node.SubscriptionId);
            Assert.AreEqual(TestResourceGroupName, node.ResourceGroupName);
            Assert.AreEqual(TestVaultUri, node.VaultUri);
            Assert.AreEqual(KeyVaultState.Succeeded, node.State);
            Assert.AreEqual("Succeeded", node.Description);
        }

        [TestMethod]
        public void Constructor_WithNullVaultUri_ConstructsDefaultUri()
        {
            // Arrange & Act
            var node = new KeyVaultNode(TestName, TestSubscriptionId, TestResourceGroupName, "Succeeded", vaultUri: null);

            // Assert
            Assert.AreEqual($"https://{TestName}.vault.azure.net/", node.VaultUri);
        }

        [TestMethod]
        public void Constructor_WithUnknownState_SetsUnknown()
        {
            // Arrange & Act
            var node = new KeyVaultNode(TestName, TestSubscriptionId, TestResourceGroupName, "InvalidState", TestVaultUri);

            // Assert
            Assert.AreEqual(KeyVaultState.Unknown, node.State);
        }

        [TestMethod]
        public void Constructor_WithNullState_SetsUnknown()
        {
            // Arrange & Act
            var node = new KeyVaultNode(TestName, TestSubscriptionId, TestResourceGroupName, state: null, TestVaultUri);

            // Assert
            Assert.AreEqual(KeyVaultState.Unknown, node.State);
        }

        #endregion

        #region State Property Tests

        [TestMethod]
        public void State_Set_UpdatesDescription()
        {
            // Arrange
            var node = new KeyVaultNode(TestName, TestSubscriptionId, TestResourceGroupName, "Succeeded", TestVaultUri);

            // Act
            node.State = KeyVaultState.Failed;

            // Assert
            Assert.AreEqual("Failed", node.Description);
        }

        [TestMethod]
        public void State_Set_RaisesPropertyChangedForIconMoniker()
        {
            // Arrange
            var node = new KeyVaultNode(TestName, TestSubscriptionId, TestResourceGroupName, "Succeeded", TestVaultUri);
            var iconMonikerChanged = false;

            node.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == "IconMoniker")
                    iconMonikerChanged = true;
            };

            // Act
            node.State = KeyVaultState.Failed;

            // Assert
            Assert.IsTrue(iconMonikerChanged);
        }

        #endregion
    }
}
