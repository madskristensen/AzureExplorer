using AzureExplorer.Core.Models;

namespace AzureExplorer.Test.Core.Models
{
    [TestClass]
    public sealed class ProvisioningStateParserTests
    {
        #region Parse Tests

        [TestMethod]
        public void Parse_WithSucceeded_ReturnsSucceeded()
        {
            // Act
            ProvisioningState result = ProvisioningStateParser.Parse("Succeeded");

            // Assert
            Assert.AreEqual(ProvisioningState.Succeeded, result);
        }

        [TestMethod]
        public void Parse_WithSucceeded_CaseInsensitive()
        {
            // Act & Assert
            Assert.AreEqual(ProvisioningState.Succeeded, ProvisioningStateParser.Parse("succeeded"));
            Assert.AreEqual(ProvisioningState.Succeeded, ProvisioningStateParser.Parse("SUCCEEDED"));
            Assert.AreEqual(ProvisioningState.Succeeded, ProvisioningStateParser.Parse("SuCcEeDeD"));
        }

        [TestMethod]
        public void Parse_WithFailed_ReturnsFailed()
        {
            // Act
            ProvisioningState result = ProvisioningStateParser.Parse("Failed");

            // Assert
            Assert.AreEqual(ProvisioningState.Failed, result);
        }

        [TestMethod]
        public void Parse_WithFailed_CaseInsensitive()
        {
            // Act & Assert
            Assert.AreEqual(ProvisioningState.Failed, ProvisioningStateParser.Parse("failed"));
            Assert.AreEqual(ProvisioningState.Failed, ProvisioningStateParser.Parse("FAILED"));
        }

        [TestMethod]
        public void Parse_WithNull_ReturnsUnknown()
        {
            // Act
            ProvisioningState result = ProvisioningStateParser.Parse(null);

            // Assert
            Assert.AreEqual(ProvisioningState.Unknown, result);
        }

        [TestMethod]
        public void Parse_WithEmptyString_ReturnsUnknown()
        {
            // Act
            ProvisioningState result = ProvisioningStateParser.Parse(string.Empty);

            // Assert
            Assert.AreEqual(ProvisioningState.Unknown, result);
        }

        [TestMethod]
        public void Parse_WithUnknownValue_ReturnsUnknown()
        {
            // Act & Assert
            Assert.AreEqual(ProvisioningState.Unknown, ProvisioningStateParser.Parse("Creating"));
            Assert.AreEqual(ProvisioningState.Unknown, ProvisioningStateParser.Parse("Deleting"));
            Assert.AreEqual(ProvisioningState.Unknown, ProvisioningStateParser.Parse("InvalidState"));
            Assert.AreEqual(ProvisioningState.Unknown, ProvisioningStateParser.Parse("random garbage"));
        }

        #endregion
    }
}
