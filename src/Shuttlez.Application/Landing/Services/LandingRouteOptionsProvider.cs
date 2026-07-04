using Shuttlez.Application.RouteRequests.DTOs;



namespace Shuttlez.Application.Landing.Services;



/// <summary>Shared dropdown options for landing + authenticated route-request forms.</summary>

public static class LandingRouteOptionsProvider

{

    private static readonly IReadOnlyList<string> UsageDayOptionsAr =

    [

        "السبت - الخميس",

        "السبت - الأربعاء",

        "الأحد - الخميس",

        "يومي (عدا الجمعة)",

        "عطلة نهاية الأسبوع فقط",

    ];



    private static readonly IReadOnlyList<string> UsageDayOptionsEn =

    [

        "Saturday - Thursday",

        "Saturday - Wednesday",

        "Sunday - Thursday",

        "Daily (except Friday)",

        "Weekends only",

    ];



    private static readonly IReadOnlyList<string> UsageReasonOptionsAr =

        ["العمل", "الدراسة", "مواعيد شخصية", "تسوق", "أخرى"];



    private static readonly IReadOnlyList<string> UsageReasonOptionsEn =

        ["Work", "Study", "Personal appointments", "Shopping", "Other"];



    public static RouteRequestOptionsDto Build(string? language = null)

    {

        var isEnglish = EgyptRouteLocations.IsEnglish(language);



        return new RouteRequestOptionsDto(

            isEnglish ? EgyptRouteLocations.EnglishCities : EgyptRouteLocations.Cities,

            isEnglish ? EgyptRouteLocations.EnglishRegionsByCity : EgyptRouteLocations.RegionsByCity,

            isEnglish ? UsageDayOptionsEn : UsageDayOptionsAr,

            isEnglish ? UsageReasonOptionsEn : UsageReasonOptionsAr);

    }

}

