namespace StudentRegistrar.Smoke.Tests;

internal static class SmokeTestSettings
{
    public static string RequireEnv(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{name} is not set. Smoke tests require this value.");
        }

        return value.TrimEnd('/');
    }

    public static string? OptionalEnv(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value.TrimEnd('/');
    }
}
