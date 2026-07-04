using System.Text.Json;



namespace Shuttlez.Application.Landing.Services;



/// <summary>مدن ومناطق مصر لنماذج طلب الخط في صفحة الهبوط.</summary>

public static class EgyptRouteLocations

{

    private static readonly Lazy<LocationData> ArabicCached = new(() => Load("egypt-locations.json"));

    private static readonly Lazy<LocationData> EnglishCached = new(() => Load("egypt-locations.en.json"));



    public static IReadOnlyList<string> Cities => ArabicCached.Value.Cities;



    public static IReadOnlyDictionary<string, IReadOnlyList<string>> RegionsByCity =>

        ArabicCached.Value.RegionsByCity;



    public static IReadOnlyList<string> EnglishCities => EnglishCached.Value.Cities;



    public static IReadOnlyDictionary<string, IReadOnlyList<string>> EnglishRegionsByCity =>

        EnglishCached.Value.RegionsByCity;



    public static bool IsEnglish(string? language) =>

        language?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true;



    private static LocationData Load(string fileName)

    {

        var path = Path.Combine(AppContext.BaseDirectory, "Landing", "Data", fileName);

        if (!File.Exists(path))

        {

            path = Path.Combine(

                Directory.GetCurrentDirectory(),

                "Landing",

                "Data",

                fileName);

        }



        if (!File.Exists(path))

        {

            throw new FileNotFoundException(

                "Egypt locations data file was not found.",

                path);

        }



        var json = File.ReadAllText(path);

        var payload = JsonSerializer.Deserialize<LocationPayload>(

            json,

            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })

            ?? throw new InvalidOperationException($"Failed to parse {fileName}.");



        if (payload.RegionsByCity is null || payload.RegionsByCity.Count == 0)

        {

            throw new InvalidOperationException($"{fileName} is empty.");

        }



        var cities = payload.RegionsByCity.Keys

            .OrderBy(c => c, StringComparer.Ordinal)

            .ToList();



        var regions = payload.RegionsByCity.ToDictionary(

            pair => pair.Key,

            pair => (IReadOnlyList<string>)pair.Value

                .OrderBy(r => r, StringComparer.Ordinal)

                .ToList(),

            StringComparer.Ordinal);



        return new LocationData(cities, regions);

    }



    private sealed record LocationPayload(Dictionary<string, List<string>> RegionsByCity);



    private sealed record LocationData(

        IReadOnlyList<string> Cities,

        IReadOnlyDictionary<string, IReadOnlyList<string>> RegionsByCity);

}

