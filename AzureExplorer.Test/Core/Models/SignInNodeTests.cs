using AzureExplorer.Core.Models;

namespace AzureExplorer.Test.Core.Models
{
    [TestClass]
    public sealed class SignInNodeTests
    {
        [TestMethod]
        public void Constructor_SetsLabelCorrectly()
        {
            // Arrange & Act
            var node = new SignInNode();

            // Assert
            Assert.AreEqual("Sign in to Azure...", node.Label);
        }

        [TestMethod]
        public void ContextMenuId_ReturnsZero()
        {
            // Arrange
            var node = new SignInNode();

            // Act
            var contextMenuId = node.ContextMenuId;

            // Assert
            Assert.AreEqual(0, contextMenuId);
        }

        [TestMethod]
        public void SupportsChildren_ReturnsFalse()
        {
            // Arrange
            var node = new SignInNode();

            // Act
            var supportsChildren = node.SupportsChildren;

            // Assert
            Assert.IsFalse(supportsChildren);
        }
    }
}
