using System;
using Npgsql;

var cs = args.Length > 0
    ? args[0]
    : Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
      ?? throw new Exception("missing connection string");

await using var conn = new NpgsqlConnection(cs);
await conn.OpenAsync();

async Task Run(string label, string sql)
{
    Console.WriteLine("=== " + label + " ===");
    await using var cmd = new NpgsqlCommand(sql, conn);
    await using var r = await cmd.ExecuteReaderAsync();
    var cols = Enumerable.Range(0, r.FieldCount).Select(r.GetName).ToArray();
    Console.WriteLine(string.Join(" | ", cols));
    while (await r.ReadAsync())
    {
        var vals = new object[r.FieldCount];
        r.GetValues(vals);
        Console.WriteLine(string.Join(" | ", vals.Select(v => v?.ToString() ?? "null")));
    }
}

await Run("tables", @"
SELECT table_name FROM information_schema.tables
WHERE table_schema='public' AND table_name IN (
  'RideRequestsSet','RideFareRulesSet','GroupRequestsSet','GroupMembersSet','GroupFareRulesSet')
ORDER BY 1;");

await Run("admins", @"
SELECT ""Id"", ""Phone"", ""FullName"", ""UserType"", ""IsActive""
FROM ""UsersSet""
WHERE ""UserType""=3 AND ""IsDeleted""=false
ORDER BY ""CreatedAt"" DESC
LIMIT 10;");

await Run("passengers", @"
SELECT ""Id"", ""Phone"", ""FullName"", ""UserType"", ""IsActive""
FROM ""UsersSet""
WHERE ""UserType""=1 AND ""IsActive""=true AND ""IsDeleted""=false
ORDER BY ""CreatedAt"" DESC
LIMIT 10;");

await Run("approved_drivers", @"
SELECT d.""Id"" AS driver_id, u.""Id"" AS user_id, u.""Phone"", u.""FullName"", d.""IsActive"", d.""VerificationStatus""
FROM ""DriversSet"" d
JOIN ""UsersSet"" u ON u.""Id""=d.""UserId""
WHERE d.""IsDeleted""=false AND d.""IsActive""=true AND d.""VerificationStatus""=1
ORDER BY d.""CreatedAt"" DESC
LIMIT 10;");

await Run("fare_rules", @"
SELECT 'ride' AS kind, COUNT(*)::text FROM ""RideFareRulesSet"" WHERE ""IsDeleted""=false
UNION ALL
SELECT 'group', COUNT(*)::text FROM ""GroupFareRulesSet"" WHERE ""IsDeleted""=false;");
