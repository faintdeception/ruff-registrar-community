using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace StudentRegistrar.Api.Tests.ServiceDefaults;

public class HealthResponseBuilderTests
{
    [Fact]
    public void Build_Should_ReturnStatusAndVersion()
    {
        // Arrange
        var report = new HealthReport(new Dictionary<string, HealthReportEntry>(), TimeSpan.Zero);

        // Act
        var response = Microsoft.Extensions.Hosting.HealthResponseBuilder.Build(report, "1.2.3");

        // Assert
        Assert.Equal(HealthStatus.Healthy.ToString(), response.Status);
        Assert.Equal("1.2.3", response.Version);
    }

    [Fact]
    public void Build_WithNullVersion_Should_ReturnUnknownVersion()
    {
        // Arrange
        var report = new HealthReport(new Dictionary<string, HealthReportEntry>(), TimeSpan.Zero);

        // Act
        var response = Microsoft.Extensions.Hosting.HealthResponseBuilder.Build(report, null);

        // Assert
        Assert.Equal(HealthStatus.Healthy.ToString(), response.Status);
        Assert.Equal("unknown", response.Version);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Build_WithWhitespaceVersion_Should_ReturnUnknownVersion(string version)
    {
        // Arrange
        var report = new HealthReport(new Dictionary<string, HealthReportEntry>(), TimeSpan.Zero);

        // Act
        var response = Microsoft.Extensions.Hosting.HealthResponseBuilder.Build(report, version);

        // Assert
        Assert.Equal(HealthStatus.Healthy.ToString(), response.Status);
        Assert.Equal("unknown", response.Version);
    }

    [Theory]
    [InlineData(HealthStatus.Healthy)]
    [InlineData(HealthStatus.Degraded)]
    [InlineData(HealthStatus.Unhealthy)]
    public void Build_WithDifferentHealthStatuses_Should_ReturnCorrectStatus(HealthStatus status)
    {
        // Arrange
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["test"] = new HealthReportEntry(status, null, TimeSpan.Zero, null, null)
        };
        var report = new HealthReport(entries, status, TimeSpan.Zero);

        // Act
        var response = Microsoft.Extensions.Hosting.HealthResponseBuilder.Build(report, "1.0.0");

        // Assert
        Assert.Equal(status.ToString(), response.Status);
        Assert.Equal("1.0.0", response.Version);
    }

    [Fact]
    public void GetVersion_Should_ReturnConsistentValue()
    {
        // Act
        var version1 = Microsoft.Extensions.Hosting.HealthResponseBuilder.GetVersion();
        var version2 = Microsoft.Extensions.Hosting.HealthResponseBuilder.GetVersion();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(version1));
        Assert.Equal(version1, version2);
    }

    [Fact]
    public void GetVersion_Should_ReturnNonEmptyVersion()
    {
        // Act
        var version = Microsoft.Extensions.Hosting.HealthResponseBuilder.GetVersion();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(version));
    }
}
