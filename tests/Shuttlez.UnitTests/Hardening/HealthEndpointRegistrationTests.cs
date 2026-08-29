using Xunit;

namespace Shuttlez.UnitTests.Hardening;

/// <summary>
/// Phase 6F — /api/v1/health must not be registered twice (AmbiguousMatchException → 500).
/// </summary>
public class HealthEndpointRegistrationTests
{
    [Fact]
    public void Program_source_does_not_MapGet_duplicate_health_path()
    {
        var apiDir = FindApiProjectDir();
        var programCs = Path.Combine(apiDir, "Program.cs");
        Assert.True(File.Exists(programCs));
        var text = File.ReadAllText(programCs);
        Assert.DoesNotContain("MapGet(\"/api/v1/health\"", text);
        Assert.Contains("MapControllers()", text);
    }

    [Fact]
    public void HealthController_route_is_api_v1_health()
    {
        var apiDir = FindApiProjectDir();
        var healthCs = Path.Combine(apiDir, "Controllers", "HealthController.cs");
        Assert.True(File.Exists(healthCs));
        var text = File.ReadAllText(healthCs);
        Assert.Contains("[Route(\"api/v1/health\")]", text);
        Assert.Contains("[HttpGet]", text);
    }

    private static string FindApiProjectDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Shuttlez.API");
            if (Directory.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate Shuttlez.API project directory");
    }
}
