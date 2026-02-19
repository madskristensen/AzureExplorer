using AzureExplorer.KeyVault.Models;

namespace AzureExplorer.Test.KeyVault.Models
{
    [TestClass]
    public sealed class KeyVaultNodeTests
    {
        private const string _testName = "my-keyvault";
        private const string _testSubscriptionId = "sub-123";
        private const string _testResourceGroupName = "rg-test";
        private const string _testVaultUri = "https://my-keyvault.vault.azure.net/";

        #region Constructor Tests

        [TestMethod]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var node = new KeyVaultNode(_testName, _testSubscriptionId, _testResourceGroupName, "Succeeded", _testVaultUri);

            // Assert
            Assert.AreEqual(_testName, node.Label);
            Assert.AreEqual(_testSubscriptionId, node.SubscriptionId);
            Assert.AreEqual(_testResourceGroupName, node.ResourceGroupName);
            Assert.AreEqual(_testVaultUri, node.VaultUri);
            Assert.AreEqual(KeyVaultState.Succeeded, node.State);
            // Description is only set for non-normal states (Failed)
            Assert.IsNull(node.Description);
        }

        [TestMethod]
        public void Constructor_WithNullVaultUri_ConstructsDefaultUri()
        {
            // Arrange & Act
            var node = new KeyVaultNode(_testName, _testSubscriptionId, _testResourceGroupName, "Succeeded", vaultUri: null);

            // Assert
            Assert.AreEqual($"https://{_testName}.vault.azure.net/", node.VaultUri);
        }

        [TestMethod]
        public void Constructor_WithUnknownState_SetsUnknown()
        {
            // Arrange & Act
            var node = new KeyVaultNode(_testName, _testSubscriptionId, _testResourceGroupName, "InvalidState", _testVaultUri);

            // Assert
            Assert.AreEqual(KeyVaultState.Unknown, node.State);
        }

        [TestMethod]
        public void Constructor_WithNullState_SetsUnknown()
        {
            // Arrange & Act
            var node = new KeyVaultNode(_testName, _testSubscriptionId, _testResourceGroupName, state: null, _testVaultUri);

            // Assert
            Assert.AreEqual(KeyVaultState.Unknown, node.State);
        }

        #endregion

        #region State Property Tests

        [TestMethod]
        public void State_Set_UpdatesDescription()
        {
            // Arrange
            var node = new KeyVaultNode(_testName, _testSubscriptionId, _testResourceGroupName, "Succeeded", _testVaultUri)
            {
                // Act
                State = KeyVaultState.Failed
            };

            // Assert
            Assert.AreEqual("Failed", node.Description);
        }

        [TestMethod]
        public void State_Set_RaisesPropertyChangedForIconMoniker()
        {
            // Arrange
            var node = new KeyVaultNode(_testName, _testSubscriptionId, _testResourceGroupName, "Succeeded", _testVaultUri);
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
