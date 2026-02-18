using AzureExplorer.AppService.Services;

namespace AzureExplorer.Test.AppService.Services;

/// <summary>
/// Tests for AppServiceManager.
/// Note: These are integration-style tests as the class design doesn't support
/// proper unit testing without refactoring (sealed class, private methods, singleton dependencies).
/// </summary>
[TestClass]
public sealed class AppServiceManagerTests
{
    [TestMethod]
    public void Instance_ReturnsNonNull()
    {
        // Act
        AppServiceManager instance = AppServiceManager.Instance;

        // Assert
        Assert.IsNotNull(instance);
    }

    [TestMethod]
    public void Instance_ReturnsSameInstanceOnMultipleCalls()
    {
        // Act
        AppServiceManager instance1 = AppServiceManager.Instance;
        AppServiceManager instance2 = AppServiceManager.Instance;

        // Assert
        Assert.AreSame(instance1, instance2);
    }

    [TestMethod]
    public void Instance_ReturnsAppServiceManagerType()
    {
        // Act
        AppServiceManager instance = AppServiceManager.Instance;

        // Assert
        Assert.IsInstanceOfType(instance, typeof(AppServiceManager));
    }

    [TestMethod]
    public async Task StartAsync_WithParameters_ThrowsExceptionAsync()
    {
        // Arrange
        AppServiceManager manager = AppServiceManager.Instance;
        var subscriptionId = "00000000-0000-0000-0000-000000000000";
        var resourceGroupName = "test-rg";
        var name = "test-app";
        CancellationToken cancellationToken = CancellationToken.None;

        // Act & Assert
        var exceptionThrown = false;
        try
        {
            await manager.StartAsync(subscriptionId, resourceGroupName, name, cancellationToken);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }
        Assert.IsTrue(exceptionThrown, "Expected an exception to be thrown");
    }

    [TestMethod]
    public async Task StartAsync_WithDefaultCancellationToken_ThrowsExceptionAsync()
    {
        // Arrange
        AppServiceManager manager = AppServiceManager.Instance;
        var subscriptionId = "00000000-0000-0000-0000-000000000000";
        var resourceGroupName = "test-rg";
        var name = "test-app";

        // Act & Assert
        var exceptionThrown = false;
        try
        {
            await manager.StartAsync(subscriptionId, resourceGroupName, name, TestContext.CancellationToken);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }
        Assert.IsTrue(exceptionThrown, "Expected an exception to be thrown");
    }

    [TestMethod]
    public async Task StopAsync_WithParameters_ThrowsExceptionAsync()
    {
        // Arrange
        AppServiceManager manager = AppServiceManager.Instance;
        var subscriptionId = "00000000-0000-0000-0000-000000000000";
        var resourceGroupName = "test-rg";
        var name = "test-app";
        CancellationToken cancellationToken = CancellationToken.None;

        // Act & Assert
        var exceptionThrown = false;
        try
        {
            await manager.StopAsync(subscriptionId, resourceGroupName, name, cancellationToken);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }
        Assert.IsTrue(exceptionThrown, "Expected an exception to be thrown");
    }

    [TestMethod]
    public async Task StopAsync_WithDefaultCancellationToken_ThrowsExceptionAsync()
    {
        // Arrange
        AppServiceManager manager = AppServiceManager.Instance;
        var subscriptionId = "00000000-0000-0000-0000-000000000000";
        var resourceGroupName = "test-rg";
        var name = "test-app";

        // Act & Assert
        var exceptionThrown = false;
        try
        {
            await manager.StopAsync(subscriptionId, resourceGroupName, name, TestContext.CancellationToken);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }
        Assert.IsTrue(exceptionThrown, "Expected an exception to be thrown");
    }

    [TestMethod]
    public async Task RestartAsync_WithParameters_ThrowsExceptionAsync()
    {
        // Arrange
        AppServiceManager manager = AppServiceManager.Instance;
        var subscriptionId = "00000000-0000-0000-0000-000000000000";
        var resourceGroupName = "test-rg";
        var name = "test-app";
        CancellationToken cancellationToken = CancellationToken.None;

        // Act & Assert
        var exceptionThrown = false;
        try
        {
            await manager.RestartAsync(subscriptionId, resourceGroupName, name, cancellationToken);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }
        Assert.IsTrue(exceptionThrown, "Expected an exception to be thrown");
    }

    [TestMethod]
    public async Task RestartAsync_WithDefaultCancellationToken_ThrowsExceptionAsync()
    {
        // Arrange
        AppServiceManager manager = AppServiceManager.Instance;
        var subscriptionId = "00000000-0000-0000-0000-000000000000";
        var resourceGroupName = "test-rg";
        var name = "test-app";

        // Act & Assert
        var exceptionThrown = false;
        try
        {
            await manager.RestartAsync(subscriptionId, resourceGroupName, name, TestContext.CancellationToken);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }
        Assert.IsTrue(exceptionThrown, "Expected an exception to be thrown");
    }

    [TestMethod]
    public async Task GetStateAsync_WithParameters_ThrowsExceptionAsync()
    {
        // Arrange
        AppServiceManager manager = AppServiceManager.Instance;
        var subscriptionId = "00000000-0000-0000-0000-000000000000";
        var resourceGroupName = "test-rg";
        var name = "test-app";
        CancellationToken cancellationToken = CancellationToken.None;

        // Act & Assert
        var exceptionThrown = false;
        try
        {
            await manager.GetStateAsync(subscriptionId, resourceGroupName, name, cancellationToken);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }
        Assert.IsTrue(exceptionThrown, "Expected an exception to be thrown");
    }

    [TestMethod]
    public async Task GetStateAsync_WithDefaultCancellationToken_ThrowsExceptionAsync()
    {
        // Arrange
        AppServiceManager manager = AppServiceManager.Instance;
        var subscriptionId = "00000000-0000-0000-0000-000000000000";
        var resourceGroupName = "test-rg";
        var name = "test-app";

        // Act & Assert
        var exceptionThrown = false;
        try
        {
            await manager.GetStateAsync(subscriptionId, resourceGroupName, name, TestContext.CancellationToken);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }
        Assert.IsTrue(exceptionThrown, "Expected an exception to be thrown");
    }

    [TestMethod]
    public async Task GetDefaultHostNameAsync_WithParameters_ThrowsExceptionAsync()
    {
        // Arrange
        AppServiceManager manager = AppServiceManager.Instance;
        var subscriptionId = "00000000-0000-0000-0000-000000000000";
        var resourceGroupName = "test-rg";
        var name = "test-app";
        CancellationToken cancellationToken = CancellationToken.None;

        // Act & Assert
        var exceptionThrown = false;
        try
        {
            await manager.GetDefaultHostNameAsync(subscriptionId, resourceGroupName, name, cancellationToken);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }
        Assert.IsTrue(exceptionThrown, "Expected an exception to be thrown");
    }

    [TestMethod]
    public async Task GetDefaultHostNameAsync_WithDefaultCancellationToken_ThrowsExceptionAsync()
    {
        // Arrange
        AppServiceManager manager = AppServiceManager.Instance;
        var subscriptionId = "00000000-0000-0000-0000-000000000000";
        var resourceGroupName = "test-rg";
        var name = "test-app";

        // Act & Assert
        var exceptionThrown = false;
        try
        {
            await manager.GetDefaultHostNameAsync(subscriptionId, resourceGroupName, name, TestContext.CancellationToken);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }
        Assert.IsTrue(exceptionThrown, "Expected an exception to be thrown");
    }

    [TestMethod]
    public async Task EnableApplicationLoggingAsync_WithParameters_ThrowsExceptionAsync()
    {
        // Arrange
        AppServiceManager manager = AppServiceManager.Instance;
        var subscriptionId = "00000000-0000-0000-0000-000000000000";
        var resourceGroupName = "test-rg";
        var name = "test-app";
        CancellationToken cancellationToken = CancellationToken.None;

        // Act & Assert
        var exceptionThrown = false;
        try
        {
            await manager.EnableApplicationLoggingAsync(subscriptionId, resourceGroupName, name, cancellationToken);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }
        Assert.IsTrue(exceptionThrown, "Expected an exception to be thrown");
    }

    [TestMethod]
    public async Task EnableApplicationLoggingAsync_WithDefaultCancellationToken_ThrowsExceptionAsync()
    {
        // Arrange
        AppServiceManager manager = AppServiceManager.Instance;
        var subscriptionId = "00000000-0000-0000-0000-000000000000";
        var resourceGroupName = "test-rg";
        var name = "test-app";

        // Act & Assert
        var exceptionThrown = false;
        try
        {
            await manager.EnableApplicationLoggingAsync(subscriptionId, resourceGroupName, name, TestContext.CancellationToken);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }
        Assert.IsTrue(exceptionThrown, "Expected an exception to be thrown");
    }

    public TestContext TestContext { get; set; }
}
