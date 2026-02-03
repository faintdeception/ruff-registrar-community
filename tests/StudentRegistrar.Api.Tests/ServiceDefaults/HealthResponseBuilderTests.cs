using FluentAssertions;
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
        response.Status.Should().Be(HealthStatus.Healthy.ToString());
        response.Version.Should().Be("1.2.3");
    }

    [Fact]
    public void Build_WithNullVersion_Should_ReturnUnknownVersion()
    {
        // Arrange
        var report = new HealthReport(new Dictionary<string, HealthReportEntry>(), TimeSpan.Zero);

        // Act
        var response = Microsoft.Extensions.Hosting.HealthResponseBuilder.Build(report, null);

        // Assert
        response.Status.Should().Be(HealthStatus.Healthy.ToString());
        response.Version.Should().Be("unknown");
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
        response.Status.Should().Be(HealthStatus.Healthy.ToString());
        response.Version.Should().Be("unknown");
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
        response.Status.Should().Be(status.ToString());
        response.Version.Should().Be("1.0.0");
    }

    [Fact]
    public void GetVersion_Should_ReturnConsistentValue()
    {
        // Act
        var version1 = Microsoft.Extensions.Hosting.HealthResponseBuilder.GetVersion();
        var version2 = Microsoft.Extensions.Hosting.HealthResponseBuilder.GetVersion();

        // Assert
        version1.Should().NotBeNullOrWhiteSpace();
        version2.Should().Be(version1);
    }

    [Fact]
    public void GetVersion_Should_ReturnNonEmptyVersion()
    {
        // Act
        var version = Microsoft.Extensions.Hosting.HealthResponseBuilder.GetVersion();

        // Assert
        version.Should().NotBeNullOrWhiteSpace();
    }
}
