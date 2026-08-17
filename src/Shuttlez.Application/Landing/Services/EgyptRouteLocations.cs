using System.Text.Json;

namespace Shuttlez.Application.Landing.Services;

/// <summary>مدن ومناطق وأحياء مصر لنماذج طلب الخط (Landing + App).</summary>
public static class EgyptRouteLocations
{
    private const string EnglishLanguage = "en";
    private static readonly Lazy<LocationData> ArabicCached = new(LoadArabic);
    private static readonly Lazy<LocationData> EnglishCached = new(BuildEnglish);

    public static IReadOnlyList<string> Cities => ArabicCached.Value.Cities;

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> RegionsByCity =>
        ArabicCached.Value.RegionsByCity;

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> AreasByRegion =>
        ArabicCached.Value.AreasByRegion;

    public static IReadOnlyList<string> EnglishCities => EnglishCached.Value.Cities;

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> EnglishRegionsByCity =>
        EnglishCached.Value.RegionsByCity;

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> EnglishAreasByRegion =>
        EnglishCached.Value.AreasByRegion;

    public static bool IsEnglish(string? language) =>
        language?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true;

    private static LocationData LoadArabic()
    {
        // الكتالوج فيه أحياء؛ egypt-locations أغنى في أحياء/أقسام القاهرة.
        var catalog = TryLoadPayload("egypt-location-catalog.json");
        var legacy = TryLoadPayload("egypt-locations.json");

        if (catalog is null && legacy is null)
        {
            throw new FileNotFoundException(
                "Egypt location data files were not found (egypt-location-catalog.json / egypt-locations.json).");
        }

        var regionsByCity = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var areasByRegion = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        MergeRegions(regionsByCity, legacy?.RegionsByCity);
        MergeRegions(regionsByCity, catalog?.RegionsByCity);
        MergeAreas(areasByRegion, catalog?.AreasByRegion);
        MergeAreas(areasByRegion, legacy?.AreasByRegion);

        var cities = regionsByCity.Keys
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToList();

        var regions = regionsByCity.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<string>)pair.Value
                .Distinct(StringComparer.Ordinal)
                .OrderBy(r => r, StringComparer.Ordinal)
                .ToList(),
            StringComparer.Ordinal);

        var areas = areasByRegion.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<string>)pair.Value
                .Distinct(StringComparer.Ordinal)
                .OrderBy(a => a, StringComparer.Ordinal)
                .ToList(),
            StringComparer.Ordinal);

        return new LocationData(cities, regions, areas);
    }

    private static void MergeRegions(
        Dictionary<string, List<string>> target,
        Dictionary<string, List<string>>? source)
    {
        if (source is null) return;
        foreach (var (city, regions) in source)
        {
            if (!target.TryGetValue(city, out var list))
            {
                list = [];
                target[city] = list;
            }

            foreach (var region in regions)
            {
                if (!list.Contains(region, StringComparer.Ordinal))
                {
                    list.Add(region);
                }
            }
        }
    }

    private static void MergeAreas(
        Dictionary<string, List<string>> target,
        Dictionary<string, List<string>>? source)
    {
        if (source is null) return;
        foreach (var (region, areas) in source)
        {
            if (!target.TryGetValue(region, out var list))
            {
                list = [];
                target[region] = list;
            }

            foreach (var area in areas)
            {
                if (!list.Contains(area, StringComparer.Ordinal))
                {
                    list.Add(area);
                }
            }
        }
    }

    private static LocationPayload? TryLoadPayload(string fileName)
    {
        var path = ResolveDataPath(fileName);
        if (path is null) return null;

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<LocationPayload>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private static string? ResolveDataPath(string fileName)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Landing", "Data", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "Landing", "Data", fileName),
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "Landing",
                "Data",
                fileName),
        };

        return candidates.Select(Path.GetFullPath).FirstOrDefault(File.Exists);
    }

    private static LocationData BuildEnglish()
    {
        var arabic = ArabicCached.Value;

        var regions = arabic.RegionsByCity.ToDictionary(
            pair => LandingLabelLocalizer.Localize(pair.Key, EnglishLanguage),
            pair => (IReadOnlyList<string>)pair.Value
                .Select(region => LandingLabelLocalizer.Localize(region, EnglishLanguage))
                .OrderBy(region => region, StringComparer.Ordinal)
                .ToList(),
            StringComparer.Ordinal);

        var areas = arabic.AreasByRegion.ToDictionary(
            pair => LandingLabelLocalizer.Localize(pair.Key, EnglishLanguage),
            pair => (IReadOnlyList<string>)pair.Value
                .Select(area => LandingLabelLocalizer.Localize(area, EnglishLanguage))
                .OrderBy(area => area, StringComparer.Ordinal)
                .ToList(),
            StringComparer.Ordinal);

        var cities = regions.Keys
            .OrderBy(city => city, StringComparer.Ordinal)
            .ToList();

        return new LocationData(cities, regions, areas);
    }

    private sealed class LocationPayload
    {
        public List<string>? Cities { get; set; }
        public Dictionary<string, List<string>>? RegionsByCity { get; set; }
        public Dictionary<string, List<string>>? AreasByRegion { get; set; }
    }

    private sealed record LocationData(
        IReadOnlyList<string> Cities,
        IReadOnlyDictionary<string, IReadOnlyList<string>> RegionsByCity,
        IReadOnlyDictionary<string, IReadOnlyList<string>> AreasByRegion);
}
