using AzureExplorer.Core.Models;

namespace AzureExplorer.Test.Core.Models
{
    [TestClass]
    public sealed class WebSiteStateTests
    {
        #region ParseState Tests

        [TestMethod]
        public void ParseState_WithRunning_ReturnsRunning()
        {
            // Act
            WebSiteState result = WebSiteNodeBase.ParseState("Running");

            // Assert
            Assert.AreEqual(WebSiteState.Running, result);
        }

        [TestMethod]
        public void ParseState_WithRunning_CaseInsensitive()
        {
            // Act & Assert
            Assert.AreEqual(WebSiteState.Running, WebSiteNodeBase.ParseState("running"));
            Assert.AreEqual(WebSiteState.Running, WebSiteNodeBase.ParseState("RUNNING"));
            Assert.AreEqual(WebSiteState.Running, WebSiteNodeBase.ParseState("RuNnInG"));
        }

        [TestMethod]
        public void ParseState_WithStopped_ReturnsStopped()
        {
            // Act
            WebSiteState result = WebSiteNodeBase.ParseState("Stopped");

            // Assert
            Assert.AreEqual(WebSiteState.Stopped, result);
        }

        [TestMethod]
        public void ParseState_WithStopped_CaseInsensitive()
        {
            // Act & Assert
            Assert.AreEqual(WebSiteState.Stopped, WebSiteNodeBase.ParseState("stopped"));
            Assert.AreEqual(WebSiteState.Stopped, WebSiteNodeBase.ParseState("STOPPED"));
        }

        [TestMethod]
        public void ParseState_WithNull_ReturnsUnknown()
        {
            // Act
            WebSiteState result = WebSiteNodeBase.ParseState(null);

            // Assert
            Assert.AreEqual(WebSiteState.Unknown, result);
        }

        [TestMethod]
        public void ParseState_WithEmptyString_ReturnsUnknown()
        {
            // Act
            WebSiteState result = WebSiteNodeBase.ParseState(string.Empty);

            // Assert
            Assert.AreEqual(WebSiteState.Unknown, result);
        }

        [TestMethod]
        public void ParseState_WithUnknownValue_ReturnsUnknown()
        {
            // Act & Assert
            Assert.AreEqual(WebSiteState.Unknown, WebSiteNodeBase.ParseState("Starting"));
            Assert.AreEqual(WebSiteState.Unknown, WebSiteNodeBase.ParseState("Restarting"));
            Assert.AreEqual(WebSiteState.Unknown, WebSiteNodeBase.ParseState("InvalidState"));
        }

        #endregion
    }
}
