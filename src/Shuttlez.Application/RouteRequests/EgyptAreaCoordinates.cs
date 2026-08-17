namespace Shuttlez.Application.RouteRequests;

/// <summary>
/// إحداثيات تقريبية لمناطق القاهرة الكبرى المستخدمة في نموذج طلب المسار
/// حتى يمكن تجميع الطلبات على الـ polyline بدون Google Geocoding.
/// </summary>
public static class EgyptAreaCoordinates
{
    private static readonly Dictionary<string, (double Lat, double Lng)> ByName =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["الشروق"] = (30.1219, 31.4956),
            ["مدينة الشروق"] = (30.1219, 31.4956),
            ["مدينة نصر"] = (30.0561, 31.3302),
            ["زهراء مدينة نصر"] = (30.0480, 31.3500),
            ["حلوان"] = (29.8492, 31.3342),
            ["المعادي"] = (29.9602, 31.2769),
            ["مصر الجديدة"] = (30.0910, 31.3240),
            ["التجمع الخامس"] = (30.0080, 31.4410),
            ["التجمع الاول"] = (30.0200, 31.4100),
            ["العاصمة الإدارية"] = (30.0200, 31.7000),
            ["العبور"] = (30.2000, 31.4700),
            ["بدر"] = (30.1330, 31.7200),
            ["الرحاب"] = (30.0600, 31.4900),
            ["مدينتي"] = (30.0850, 31.6400),
            ["الشيخ زايد"] = (30.0400, 31.0100),
            ["6 أكتوبر"] = (29.9285, 30.9188),
            ["الجيزة"] = (30.0131, 31.2089),
            ["الدقي"] = (30.0380, 31.2120),
            ["المهندسين"] = (30.0480, 31.2000),
            ["وسط البلد"] = (30.0444, 31.2357),
            ["القاهرة"] = (30.0444, 31.2357),
            ["العباسية"] = (30.0650, 31.2750),
            ["عين شمس"] = (30.1300, 31.3300),
            ["المطرية"] = (30.1150, 31.3000),
            ["شبرا"] = (30.0800, 31.2400),
            ["شبرا الخيمة"] = (30.1280, 31.2420),
            ["حدائق القبة"] = (30.0900, 31.2800),
            ["النزهة"] = (30.1000, 31.3500),
            ["المقطم"] = (30.0100, 31.3100),
            ["البساتين"] = (29.9900, 31.2700),
            ["دار السلام"] = (29.9800, 31.2500),
            ["طرة"] = (29.9200, 31.2800),
            ["صفط اللبن"] = (30.0200, 31.1700),
            ["فيصل"] = (30.0100, 31.1800),
            ["الهرم"] = (29.9900, 31.1400),
            ["الوايلي"] = (30.0700, 31.2700),
            ["الزمالك"] = (30.0600, 31.2200),
            ["جاردن سيتي"] = (30.0400, 31.2300),
        };

    public static (double Lat, double Lng)? Resolve(string? region, string? city)
    {
        if (!string.IsNullOrWhiteSpace(region) && ByName.TryGetValue(region.Trim(), out var byRegion))
        {
            return byRegion;
        }

        if (!string.IsNullOrWhiteSpace(city) && ByName.TryGetValue(city.Trim(), out var byCity))
        {
            return byCity;
        }

        // محاولة جزئية على النص المركّب.
        var haystack = $"{region} {city}".Trim();
        foreach (var pair in ByName)
        {
            if (haystack.Contains(pair.Key, StringComparison.OrdinalIgnoreCase))
            {
                return pair.Value;
            }
        }

        return null;
    }

    public static bool HasValidCoordinates(double lat, double lng) =>
        Math.Abs(lat) > 0.01 || Math.Abs(lng) > 0.01;
}
