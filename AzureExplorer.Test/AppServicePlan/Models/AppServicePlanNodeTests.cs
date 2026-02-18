using AzureExplorer.AppServicePlan.Models;

namespace AzureExplorer.Test.AppServicePlan.Models
{
    [TestClass]
    public sealed class AppServicePlanNodeTests
    {
        private const string TestName = "my-app-service-plan";
        private const string TestSubscriptionId = "sub-123";
        private const string TestResourceGroupName = "rg-test";

        [TestMethod]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var node = new AppServicePlanNode(TestName, TestSubscriptionId, TestResourceGroupName, "P1v2", "linux", numberOfSites: 3);

            // Assert
            Assert.AreEqual(TestName, node.Label);
            Assert.AreEqual(TestSubscriptionId, node.SubscriptionId);
            Assert.AreEqual(TestResourceGroupName, node.ResourceGroupName);
            Assert.AreEqual("P1v2", node.Sku);
            Assert.AreEqual("linux", node.Kind);
            Assert.AreEqual(3, node.NumberOfSites);
        }

        [TestMethod]
        public void Constructor_WithNullNumberOfSites_SetsZero()
        {
            // Arrange & Act
            var node = new AppServicePlanNode(TestName, TestSubscriptionId, TestResourceGroupName, "P1v2", "linux", numberOfSites: null);

            // Assert
            Assert.AreEqual(0, node.NumberOfSites);
        }

        [TestMethod]
        public void Constructor_BuildsDescription_WithSkuAndSites()
        {
            // Arrange & Act
            var node = new AppServicePlanNode(TestName, TestSubscriptionId, TestResourceGroupName, "P1v2", "linux", numberOfSites: 3);

            // Assert
            Assert.AreEqual("P1v2 | 3 sites", node.Description);
        }

        [TestMethod]
        public void Constructor_WithOneSite_UsesSingularForm()
        {
            // Arrange & Act
            var node = new AppServicePlanNode(TestName, TestSubscriptionId, TestResourceGroupName, "P1v2", "linux", numberOfSites: 1);

            // Assert
            Assert.AreEqual("P1v2 | 1 site", node.Description);
        }

        [TestMethod]
        public void Constructor_WithEmptyOrNullSku_OnlyShowsSites()
        {
            // Arrange & Act
            var nodeEmpty = new AppServicePlanNode(TestName, TestSubscriptionId, TestResourceGroupName, sku: "", "linux", numberOfSites: 2);
            var nodeNull = new AppServicePlanNode(TestName, TestSubscriptionId, TestResourceGroupName, sku: null, "linux", numberOfSites: 2);

            // Assert
            Assert.AreEqual("2 sites", nodeEmpty.Description);
            Assert.AreEqual("2 sites", nodeNull.Description);
        }
    }
}
