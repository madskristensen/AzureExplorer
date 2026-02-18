using AzureExplorer.KeyVault.Models;

namespace AzureExplorer.Test.KeyVault.Models
{
    [TestClass]
    public sealed class SecretNodeTests
    {
        private const string _testName = "my-secret";
        private const string _testSubscriptionId = "sub-123";
        private const string _testVaultUri = "https://my-keyvault.vault.azure.net/";

        [TestMethod]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var enabledNode = new SecretNode(_testName, _testSubscriptionId, _testVaultUri, enabled: true);
            var disabledNode = new SecretNode(_testName, _testSubscriptionId, _testVaultUri, enabled: false);

            // Assert
            Assert.AreEqual(_testName, enabledNode.Label);
            Assert.AreEqual(_testSubscriptionId, enabledNode.SubscriptionId);
            Assert.AreEqual(_testVaultUri, enabledNode.VaultUri);
            Assert.IsTrue(enabledNode.Enabled);
            Assert.AreEqual("Enabled", enabledNode.Description);

            Assert.IsFalse(disabledNode.Enabled);
            Assert.AreEqual("Disabled", disabledNode.Description);
        }

        [TestMethod]
        public void SecretId_ReturnsCorrectUrl()
        {
            // Arrange
            var node = new SecretNode(_testName, _testSubscriptionId, _testVaultUri, enabled: true);

            // Act
            var secretId = node.SecretId;

            // Assert
            Assert.AreEqual("https://my-keyvault.vault.azure.net/secrets/my-secret", secretId);
        }

        [TestMethod]
        public void SecretId_WithTrailingSlashOnVaultUri_ReturnsCorrectUrl()
        {
            // Arrange - VaultUri already has trailing slash
            var node = new SecretNode(_testName, _testSubscriptionId, "https://my-keyvault.vault.azure.net/", enabled: true);

            // Act
            var secretId = node.SecretId;

            // Assert - Should not have double slashes
            Assert.AreEqual("https://my-keyvault.vault.azure.net/secrets/my-secret", secretId);
        }
    }
}
