using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shuttlez.Application;
using Shuttlez.Infrastructure;
using Shuttlez.Infrastructure.Data;

Console.OutputEncoding = Encoding.UTF8;
var apiDir = Path.GetFullPath(@"F:\aa_MOC\Flutter_Projects\shuttlez-cursor-api\src\Shuttlez.API");
var config = new ConfigurationBuilder()
    .SetBasePath(apiDir)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(config);
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Error));
services.AddApplication();
services.AddInfrastructure(config);
using var scope = services.BuildServiceProvider().CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await using var conn = db.Database.GetDbConnection();
await conn.OpenAsync();

async Task Q(string title, string sql)
{
    Console.WriteLine("\n=== " + title + " ===");
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = sql;
    await using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync())
    {
        var parts = new List<string>();
        for (var i = 0; i < r.FieldCount; i++)
            parts.Add(r.IsDBNull(i) ? "null" : Convert.ToString(r.GetValue(i))!);
        Console.WriteLine(string.Join(" | ", parts));
    }
}

await Q("BOOKING FINANCIAL DISTRIBUTION", """
SELECT
  COUNT(*) AS n,
  COUNT(*) FILTER (WHERE "PricePerSeat" = 0) AS pps_zero,
  COUNT(*) FILTER (WHERE "PricePerSeat" > 0) AS pps_gt0,
  COUNT(*) FILTER (WHERE "CommissionRate" = 0) AS rate_zero,
  COUNT(*) FILTER (WHERE "CommissionRate" > 0) AS rate_gt0,
  COUNT(*) FILTER (WHERE "CommissionAmount" = 0) AS amt_zero,
  COUNT(*) FILTER (WHERE "CaptainEarnings" = "TotalAmount") AS captain_eq_total,
  COUNT(*) FILTER (WHERE "CaptainEarnings" <> "TotalAmount") AS captain_ne_total,
  COUNT(*) FILTER (WHERE "SeatCount" > 0 AND "PricePerSeat" = ROUND(("TotalAmount"/"SeatCount")::numeric,2)) AS pps_matches_derived
FROM "BookingsSet"
WHERE NOT "IsDeleted"
""");

await Q("PRICING RULES COUNT", """
SELECT COUNT(*)::text, COUNT(*) FILTER (WHERE "RouteId" IS NULL)::text AS vehicle_defaults,
 COUNT(*) FILTER (WHERE "IsActive")::text AS active
FROM "PricingRulesSet" WHERE NOT "IsDeleted"
""");

await Q("PRICING RULE SUMMARY (no secrets)", """
SELECT "Name", "VehicleType", "RouteId" IS NULL AS is_default,
 "OneWayPrice", "RoundTripPrice", "WeeklyPrice", "MonthlyPrice",
 "LaunchCommissionPercent", "PermanentCommissionPercent",
 "MinimumLaunchRiders", "TargetOccupancy", "IsActive"
FROM "PricingRulesSet" WHERE NOT "IsDeleted" ORDER BY "VehicleType", "Name"
""");

await Q("COMMISSION RULES", """
SELECT "Name", "PlatformCommissionPercent", "IsActive",
 "EffectiveFrom", "EffectiveTo"
FROM "CommissionRulesSet" WHERE NOT "IsDeleted"
""");

await Q("MAPPED ROUTE COUNTS", """
SELECT COUNT(*)::text AS states,
 COUNT(*) FILTER (WHERE "MappedRouteId" IS NOT NULL)::text AS mapped
FROM "RouteDemandGroupStatesSet" WHERE NOT "IsDeleted"
""");

await Q("INDEXES ON NEW TABLES", """
SELECT tablename, indexname FROM pg_indexes
WHERE schemaname='public' AND tablename IN ('PricingRulesSet','CommissionRulesSet','RouteDemandGroupStatesSet','BookingsSet')
ORDER BY 1,2
""");

Console.WriteLine("\nDONE");
