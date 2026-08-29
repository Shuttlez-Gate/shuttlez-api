using Npgsql;
var cs = args[0];
await using var c = new NpgsqlConnection(cs);
await c.OpenAsync();
await using var cmd = new NpgsqlCommand('SELECT ""Name"", ""OneWayPrice"", ""VehicleType"", ""IsActive"" FROM ""PricingRulesSet"" WHERE ""IsDeleted""=false ORDER BY ""IsActive"" DESC LIMIT 8', c);
await using var r = await cmd.ExecuteReaderAsync();
while (await r.ReadAsync()) Console.WriteLine($""{r.GetString(0)} | {r.GetDecimal(1)} | vt={r.GetInt32(2)} | active={r.GetBoolean(3)}"");
