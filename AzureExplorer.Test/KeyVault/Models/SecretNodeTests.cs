using AzureExplorer.KeyVault.Models;

namespace AzureExplorer.Test.KeyVault.Models
{
    [TestClass]
    public sealed class SecretNodeTests
    {
        private const string TestName = "my-secret";
        private const string TestSubscriptionId = "sub-123";
        private const string TestVaultUri = "https://my-keyvault.vault.azure.net/";

        [TestMethod]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var enabledNode = new SecretNode(TestName, TestSubscriptionId, TestVaultUri, enabled: true);
            var disabledNode = new SecretNode(TestName, TestSubscriptionId, TestVaultUri, enabled: false);

            // Assert
            Assert.AreEqual(TestName, enabledNode.Label);
            Assert.AreEqual(TestSubscriptionId, enabledNode.SubscriptionId);
            Assert.AreEqual(TestVaultUri, enabledNode.VaultUri);
            Assert.IsTrue(enabledNode.Enabled);
            Assert.AreEqual("Enabled", enabledNode.Description);

            Assert.IsFalse(disabledNode.Enabled);
            Assert.AreEqual("Disabled", disabledNode.Description);
        }

        [TestMethod]
        public void SecretId_ReturnsCorrectUrl()
        {
            // Arrange
            var node = new SecretNode(TestName, TestSubscriptionId, TestVaultUri, enabled: true);

            // Act
            var secretId = node.SecretId;

            // Assert
            Assert.AreEqual("https://my-keyvault.vault.azure.net/secrets/my-secret", secretId);
        }

        [TestMethod]
        public void SecretId_WithTrailingSlashOnVaultUri_ReturnsCorrectUrl()
        {
            // Arrange - VaultUri already has trailing slash
            var node = new SecretNode(TestName, TestSubscriptionId, "https://my-keyvault.vault.azure.net/", enabled: true);

            // Act
            var secretId = node.SecretId;

            // Assert - Should not have double slashes
            Assert.AreEqual("https://my-keyvault.vault.azure.net/secrets/my-secret", secretId);
        }
    }
}
