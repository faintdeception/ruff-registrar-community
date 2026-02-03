using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

public sealed record HealthStatusResponse(
    [property: JsonPropertyName("status")] string Status, 
    [property: JsonPropertyName("version")] string Version);

public static class HealthResponseBuilder
{
    private static readonly Lazy<string> CachedVersion = new(ResolveVersion);

    public static HealthStatusResponse Build(HealthReport report, string? version)
    {
        var resolvedVersion = string.IsNullOrWhiteSpace(version) ? "unknown" : version;
        return new HealthStatusResponse(report.Status.ToString(), resolvedVersion);
    }

    public static string GetVersion() => CachedVersion.Value;

    private static string ResolveVersion()
    {
        var entryAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var informational = entryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            return informational;
        }

        var version = entryAssembly.GetName().Version?.ToString();
        return string.IsNullOrWhiteSpace(version) ? "unknown" : version;
    }
}
