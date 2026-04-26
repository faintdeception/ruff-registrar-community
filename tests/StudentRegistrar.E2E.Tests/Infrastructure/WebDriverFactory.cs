using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;

namespace StudentRegistrar.E2E.Tests.Infrastructure;

public class WebDriverFactory : IDisposable
{
    private IWebDriver? _driver;
    private readonly IConfiguration _configuration;

    public WebDriverFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IWebDriver CreateDriver()
    {
        if (_driver != null)
            return _driver;

        EnsureSeleniumManagerIsExecutable();

        var options = new ChromeOptions();
        
        // Configure Chrome options based on settings
        var headless = bool.Parse(_configuration["SeleniumSettings:Headless"] ?? "false");
        if (headless)
        {
            options.AddArgument("--headless=new"); // Use new headless mode
        }

        // Add Chrome options for better stability and crash prevention
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--disable-software-rasterizer");
        options.AddArgument("--disable-background-timer-throttling");
        options.AddArgument("--disable-backgrounding-occluded-windows");
        options.AddArgument("--disable-renderer-backgrounding");
        options.AddArgument("--disable-features=TranslateUI");
        options.AddArgument("--disable-ipc-flooding-protection");
        options.AddArgument("--window-size=1920,1080");
        options.AddArgument("--start-maximized");
        
        // Memory and performance optimizations
        options.AddArgument("--memory-pressure-off");
        options.AddArgument("--max_old_space_size=4096");
        
        // Disable extensions and plugins that can cause crashes
        options.AddArgument("--disable-extensions");
        options.AddArgument("--disable-plugins");
        options.AddArgument("--disable-default-apps");
        
        // Add user data directory to prevent profile conflicts
        var tempUserDataDir = Path.Combine(Path.GetTempPath(), $"ChromeTest_{Guid.NewGuid()}");
        options.AddArgument($"--user-data-dir={tempUserDataDir}");
        
        // Enable logging for debugging
        options.AddArgument("--enable-logging");
        options.AddArgument("--log-level=0");
        
        // Set page load strategy for better reliability
        options.PageLoadStrategy = PageLoadStrategy.Normal;

        var service = CreateChromeDriverService();
        _driver = service is null
            ? new ChromeDriver(options)
            : new ChromeDriver(service, options);

        // Configure timeouts
        var implicitWait = int.Parse(_configuration["SeleniumSettings:ImplicitWaitSeconds"] ?? "10");
        var pageLoadTimeout = int.Parse(_configuration["SeleniumSettings:PageLoadTimeoutSeconds"] ?? "30");

        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(implicitWait);
        _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(pageLoadTimeout);

        return _driver;
    }

    private static void EnsureSeleniumManagerIsExecutable()
    {
        // Selenium extracts selenium-manager next to the test output. On some systems the
        // executable bit can be lost during restore/build, causing "Permission denied".
        string? runtime = null;

        if (OperatingSystem.IsLinux())
            runtime = "linux";
        else if (OperatingSystem.IsMacOS())
            runtime = "osx";
        else
            return;

        var managerPath = Path.Combine(AppContext.BaseDirectory, "runtimes", runtime, "native", "selenium-manager");

        if (!File.Exists(managerPath))
            return;

        try
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                var mode = File.GetUnixFileMode(managerPath);
                var desired = mode | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;

                if (desired != mode)
                    File.SetUnixFileMode(managerPath, desired);
            }
        }
        catch
        {
            // Best-effort: if we can't adjust permissions, Selenium will surface a useful error.
        }
    }

    private static ChromeDriverService? CreateChromeDriverService()
    {
        var driverPath = ResolveChromeDriverPath();
        if (string.IsNullOrWhiteSpace(driverPath))
        {
            return null;
        }

        var driverDirectory = Path.GetDirectoryName(driverPath);
        var driverFileName = Path.GetFileName(driverPath);
        if (string.IsNullOrWhiteSpace(driverDirectory) || string.IsNullOrWhiteSpace(driverFileName))
        {
            return null;
        }

        var service = ChromeDriverService.CreateDefaultService(driverDirectory, driverFileName);
        service.HideCommandPromptWindow = true;
        return service;
    }

    private static string? ResolveChromeDriverPath()
    {
        var managerPath = GetSeleniumManagerPath();
        if (string.IsNullOrWhiteSpace(managerPath) || !File.Exists(managerPath))
        {
            return null;
        }

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = managerPath,
                Arguments = "--browser chrome --skip-driver-in-path --output json",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(TimeSpan.FromSeconds(60));
            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
            {
                return null;
            }

            using var doc = JsonDocument.Parse(output);
            if (!doc.RootElement.TryGetProperty("result", out var result)
                || !result.TryGetProperty("driver_path", out var driverPathElement))
            {
                return null;
            }

            var driverPath = driverPathElement.GetString();
            return !string.IsNullOrWhiteSpace(driverPath) && File.Exists(driverPath)
                ? driverPath
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? GetSeleniumManagerPath()
    {
        var executableName = OperatingSystem.IsWindows() ? "selenium-manager.exe" : "selenium-manager";
        string runtime;

        if (OperatingSystem.IsWindows())
        {
            runtime = "win";
        }
        else if (OperatingSystem.IsLinux())
        {
            runtime = "linux";
        }
        else if (OperatingSystem.IsMacOS())
        {
            runtime = "osx";
        }
        else
        {
            return null;
        }

        return Path.Combine(AppContext.BaseDirectory, "runtimes", runtime, "native", executableName);
    }

    public void Dispose()
    {
        try
        {
            // Close all windows and quit properly
            _driver?.Close();
            _driver?.Quit();
        }
        catch (Exception)
        {
            // Ignore errors during cleanup
        }
        finally
        {
            try
            {
                _driver?.Dispose();
            }
            catch (Exception)
            {
                // Ignore errors during disposal
            }
            _driver = null;
        }
    }
}
