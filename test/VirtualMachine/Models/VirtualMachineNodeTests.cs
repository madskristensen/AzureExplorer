using AzureExplorer.VirtualMachine.Models;

namespace AzureExplorer.Test.VirtualMachine.Models
{
    [TestClass]
    public sealed class VirtualMachineNodeTests
    {
        [TestMethod]
        public void Constructor_SetsPropertiesAndDescription()
        {
            // Arrange & Act
            var node = new VirtualMachineNode(
                "vm1",
                "sub-123",
                "rg1",
                "PowerState/running",
                "Standard_D2s_v3",
                "Windows",
                "52.1.2.3",
                "10.0.0.4");

            // Assert
            Assert.AreEqual(VirtualMachineState.Running, node.State);
            Assert.AreEqual(VirtualMachineOsType.Windows, node.OsType);
            Assert.AreEqual("Running • Windows • Standard_D2s_v3", node.Description);
            Assert.IsTrue(node.CanConnectRdp);
            Assert.IsFalse(node.CanConnectSsh);
            Assert.AreEqual(PackageIds.VirtualMachineContextMenu, node.ContextMenuId);
        }

        [TestMethod]
        public void Constructor_WithLinuxAndPublicIp_CanConnectSsh()
        {
            // Arrange & Act
            var node = new VirtualMachineNode(
                "vm1",
                "sub-123",
                "rg1",
                "deallocated",
                "Standard_B2s",
                "Linux",
                "52.1.2.3",
                "10.0.0.4");

            // Assert
            Assert.AreEqual(VirtualMachineState.Deallocated, node.State);
            Assert.AreEqual(VirtualMachineOsType.Linux, node.OsType);
            Assert.IsFalse(node.CanConnectRdp);
            Assert.IsTrue(node.CanConnectSsh);
        }

        [TestMethod]
        public void ParseState_ReturnsExpectedState()
        {
            // Arrange
            var testCases = new (string Input, string Expected)[]
            {
                ("PowerState/running", "Running"),
                ("running", "Running"),
                ("PowerState/stopped", "Stopped"),
                ("deallocated", "Deallocated"),
                ("", "Unknown"),
                (null, "Unknown"),
                ("invalid", "Unknown")
            };

            // Act & Assert
            foreach ((string input, string expected) in testCases)
            {
                var actual = VirtualMachineNode.ParseState(input);
                Assert.AreEqual(expected, actual.ToString());
            }
        }

        [TestMethod]
        public void ParseOsType_ReturnsExpectedType()
        {
            // Arrange
            var testCases = new (string Input, string Expected)[]
            {
                ("Windows", "Windows"),
                ("linux", "Linux"),
                ("", "Unknown"),
                (null, "Unknown"),
                ("invalid", "Unknown")
            };

            // Act & Assert
            foreach ((string input, string expected) in testCases)
            {
                var actual = VirtualMachineNode.ParseOsType(input);
                Assert.AreEqual(expected, actual.ToString());
            }
        }

        [TestMethod]
        public void UpdatePublicIpAddress_RaisesPropertyChangedEvents()
        {
            // Arrange
            var node = new VirtualMachineNode(
                "vm1",
                "sub-123",
                "rg1",
                "running",
                "Standard_D2s_v3",
                "Windows",
                publicIpAddress: null,
                "10.0.0.4");
            var changed = new List<string>();
            node.PropertyChanged += (sender, args) => changed.Add(args.PropertyName!);

            // Act
            node.UpdatePublicIpAddress("52.1.2.3");

            // Assert
            Assert.AreEqual("52.1.2.3", node.PublicIpAddress);
            Assert.Contains("PublicIpAddress", changed);
            Assert.Contains("CanConnectRdp", changed);
            Assert.Contains("CanConnectSsh", changed);
            Assert.IsTrue(node.CanConnectRdp);
        }
    }
}
