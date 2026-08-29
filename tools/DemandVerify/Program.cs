using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shuttlez.Application;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Admin.Services;
using Shuttlez.Infrastructure;
using Shuttlez.Infrastructure.Data;

// Phase 2.3 READ-ONLY production verification — no mapping writes.
Console.OutputEncoding = Encoding.UTF8;

var apiDir = Path.GetFullPath(@"F:\aa_MOC\Flutter_Projects\shuttlez-cursor-api\src\Shuttlez.API");
var outPath = Path.GetFullPath(@"F:\aa_MOC\Flutter_Projects\shuttlez-cursor-api\tools\DemandVerify\phase23-verify.md");

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
var analysis = scope.ServiceProvider.GetRequiredService<IRouteDemandAnalysisService>();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

var sb = new StringBuilder();
sb.AppendLine("# Phase 2.3 read-only verification");
sb.AppendLine();
sb.AppendLine($"GeneratedUtc: {DateTime.UtcNow:O}");
sb.AppendLine();
sb.AppendLine("**No automatic mapping was performed.**");
sb.AppendLine();

var migrations = await db.Database.SqlQueryRaw<string>("""
    SELECT "MigrationId" AS "Value" FROM "__EFMigrationsHistory"
    WHERE "MigrationId" LIKE '%Phase%' OR "MigrationId" LIKE '%Mapped%'
    ORDER BY 1
    """).ToListAsync();
sb.AppendLine("## Applied Phase migrations");
foreach (var m in migrations) sb.AppendLine("- `" + m + "`");
sb.AppendLine();

sb.AppendLine("## Capacities");
foreach (var c in await analysis.GetVehicleCapacitiesAsync(CancellationToken.None))
    sb.AppendLine($"- {c.VehicleType}: {c.Capacity} ({c.Source})");
sb.AppendLine();

var pricingCount = await db.PricingRules.CountAsync(r => !r.IsDeleted && r.IsActive);
var commissionCount = await db.CommissionRules.CountAsync(r => !r.IsDeleted && r.IsActive);
sb.AppendLine($"## Pricing active rules: {pricingCount}");
sb.AppendLine($"## Commission active rules: {commissionCount}");
sb.AppendLine();

var mappedStates = await db.RouteDemandGroupStates.CountAsync(s => !s.IsDeleted && s.MappedRouteId != null);
sb.AppendLine($"## Explicit MappedRouteId states: {mappedStates}");
sb.AppendLine();

var (items, total) = await analysis.GetRoutesAsync(new RouteDemandAnalysisQuery(Page: 1, PageSize: 200), CancellationToken.None);
sb.AppendLine($"## Demand groups: {total}");

var linked = items.Count(i => i.Readiness?.PricingLinked == true);
var missingLink = items.Count(i => i.Readiness?.ReasonCode == "MISSING_ROUTE_LINK");
var pricingOk = items.Count(i => i.Readiness?.PricingAvailable == true);
var noPricing = items.Count(i => i.Readiness?.LaunchStatus == "NO_PRICING");
var ready = items.Count(i => i.Readiness?.LaunchStatus == "READY");
var almost = items.Count(i => i.Readiness?.LaunchStatus == "ALMOST_READY");
var notReady = items.Count(i => i.Readiness?.LaunchStatus == "NOT_READY");
var unknown = items.Count(i => i.Readiness?.LaunchStatus == "UNKNOWN");
var missingCfg = items.Count(i => i.Readiness?.LaunchStatus == "MISSING_CONFIGURATION");
var conflicts = items.Count(i => i.Readiness?.HasCapacityConflict == true);
var explicitLink = items.Count(i => i.Readiness?.RouteLinkSource == "EXPLICIT");
var exactKey = items.Count(i => i.Readiness?.RouteLinkSource == "EXACT_KEY");
var manualOverride = items.Count(i => i.Readiness?.MappingCompatibility == "MANUAL_OVERRIDE");

sb.AppendLine();
sb.AppendLine("| Metric | Count |");
sb.AppendLine("|--------|-------|");
sb.AppendLine($"| Total | {total} |");
sb.AppendLine($"| PricingLinked | {linked} |");
sb.AppendLine($"| MISSING_ROUTE_LINK | {missingLink} |");
sb.AppendLine($"| EXPLICIT maps (readiness) | {explicitLink} |");
sb.AppendLine($"| EXACT_KEY | {exactKey} |");
sb.AppendLine($"| MANUAL_OVERRIDE | {manualOverride} |");
sb.AppendLine($"| Pricing available | {pricingOk} |");
sb.AppendLine($"| NO_PRICING | {noPricing} |");
sb.AppendLine($"| READY | {ready} |");
sb.AppendLine($"| ALMOST_READY | {almost} |");
sb.AppendLine($"| NOT_READY | {notReady} |");
sb.AppendLine($"| MISSING_CONFIGURATION | {missingCfg} |");
sb.AppendLine($"| UNKNOWN | {unknown} |");
sb.AppendLine($"| Capacity conflicts | {conflicts} |");
sb.AppendLine();

sb.AppendLine("## Top corridors (read-only)");
sb.AppendLine("| Route | Demand | Unique | Confirmed | Link | Status | Reason | OW | Cap | Conflict |");
sb.AppendLine("|-------|--------|--------|-----------|------|--------|--------|----|-----|----------|");
foreach (var row in items.OrderByDescending(i => i.TotalRequests).ThenBy(i => i.RouteLabel).Take(31))
{
    var rd = row.Readiness;
    sb.AppendLine(
        $"| {Escape(row.RouteLabel)} | {row.TotalRequests} | {row.UniquePassengers} | {row.ConfirmedPassengers} | {rd?.RouteLinkSource ?? "—"} | {rd?.LaunchStatus ?? "—"} | {rd?.ReasonCode ?? "—"} | {rd?.OneWayPrice?.ToString() ?? "null"} | {rd?.Capacity?.ToString() ?? "null"} | {(rd?.HasCapacityConflict == true ? "YES" : "no")} |");
}

sb.AppendLine();
sb.AppendLine("## Phase 5 — full demand audit (all groups, read-only)");
sb.AppendLine("| routeKey | origin | destination | confirmed | minRequired | status | mapped | reason |");
sb.AppendLine("|----------|--------|-------------|-----------|-------------|--------|--------|--------|");
foreach (var row in items.OrderByDescending(i => i.ConfirmedPassengers).ThenByDescending(i => i.TotalRequests).ThenBy(i => i.RouteLabel))
{
    var rd = row.Readiness;
    var mapped = rd?.PricingLinked == true
        ? $"{rd.RouteLinkSource ?? "LINKED"}"
        : (rd?.ReasonCode == "MISSING_ROUTE_LINK" ? "UNMAPPED" : "—");
    sb.AppendLine(
        $"| `{Escape(row.RouteKey)}` | {Escape(row.EndpointA)} | {Escape(row.EndpointB)} | {row.ConfirmedPassengers} | {rd?.MinimumLaunchRiders?.ToString() ?? "—"} | {rd?.LaunchStatus ?? "—"} | {mapped} | {rd?.ReasonCode ?? "—"} |");
}

var auditPath = Path.GetFullPath(@"F:\aa_MOC\Flutter_Projects\shuttlez-cursor-api\docs\PHASE5_DEMAND_READINESS_AUDIT.md");
File.WriteAllText(outPath, sb.ToString(), new UTF8Encoding(true));
File.WriteAllText(auditPath, sb.ToString(), new UTF8Encoding(true));
Console.WriteLine(sb.ToString());
Console.WriteLine("Wrote " + outPath);
Console.WriteLine("Wrote " + auditPath);

static string Escape(string s) => s.Replace("|", "/").Replace("\r", " ").Replace("\n", " ");
