using System.Text.Json;

namespace Shuttlez.Application.Landing.Services;

public static class LandingLabelLocalizer
{
    private static readonly Lazy<IReadOnlyDictionary<string, string>> Labels =
        new(LoadLabels);

    public static string Localize(string? value, string? language)
    {
        if (string.IsNullOrWhiteSpace(value) || !EgyptRouteLocations.IsEnglish(language))
        {
            return value?.Trim() ?? string.Empty;
        }

        var key = value.Trim();
        return Labels.Value.TryGetValue(key, out var translated) ? translated : key;
    }

    public static (string From, string To) LocalizeRouteName(string name, string? language)
    {
        var parts = name.Split(" - ", 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            return (Localize(name, language), Localize(name, language));
        }

        return (Localize(parts[0], language), Localize(parts[1], language));
    }

    private static IReadOnlyDictionary<string, string> LoadLabels()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Landing", "Data", "egypt-labels-map.json");
        if (!File.Exists(path))
        {
            path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Landing",
                "Data",
                "egypt-labels-map.json");
        }

        if (!File.Exists(path))
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        var json = File.ReadAllText(path);
        var payload = JsonSerializer.Deserialize<LabelsPayload>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return payload?.Labels ?? new Dictionary<string, string>(StringComparer.Ordinal);
    }

    private sealed record LabelsPayload(Dictionary<string, string> Labels);
}
