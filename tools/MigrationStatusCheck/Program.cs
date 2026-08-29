using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shuttlez.Infrastructure.Data;

var basePath = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
var cfg = new ConfigurationBuilder()
    .SetBasePath(basePath)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var cs = cfg.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(cs))
{
    Console.WriteLine("STATUS=NO_CONNECTION_STRING");
    return 2;
}

cs = cs
    .Replace("SSL Mode=VerifyFull", "SSL Mode=Require", StringComparison.OrdinalIgnoreCase)
    .Replace("Channel Binding=Require", "Channel Binding=Disable", StringComparison.OrdinalIgnoreCase);

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseNpgsql(cs, n => n.CommandTimeout(20))
    .Options;

try
{
    await using var db = new AppDbContext(options);
    var can = await db.Database.CanConnectAsync();
    Console.WriteLine($"CAN_CONNECT={can}");
    if (!can)
    {
        return 3;
    }

    var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();
    var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
    Console.WriteLine($"APPLIED_COUNT={applied.Count}");
    Console.WriteLine($"PENDING_COUNT={pending.Count}");
    Console.WriteLine($"PHASE4C_APPLIED={applied.Any(m => m.Contains("Phase4C_UserDevices", StringComparison.Ordinal))}");
    Console.WriteLine($"PHASE4C_PENDING={pending.Any(m => m.Contains("Phase4C_UserDevices", StringComparison.Ordinal))}");
    foreach (var p in pending)
    {
        Console.WriteLine($"PENDING={p}");
    }

    var hasDevicesTable = await db.Database
        .SqlQueryRaw<int>("SELECT CASE WHEN EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'UserDevicesSet') THEN 1 ELSE 0 END AS \"Value\"")
        .FirstAsync();
    Console.WriteLine($"USERDEVICES_TABLE={hasDevicesTable == 1}");
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"STATUS=BLOCKED");
    Console.WriteLine($"ERR_TYPE={ex.GetType().Name}");
    return 1;
}
