using Npgsql;
var cs = args[0];
await using var c = new NpgsqlConnection(cs);
await c.OpenAsync();
await using var cmd = new NpgsqlCommand("SELECT \"PlateNumber\", \"Seats\", \"VehicleType\", \"IsActive\" FROM \"VehiclesSet\" WHERE \"IsDeleted\"=false LIMIT 10", c);
await using var r = await cmd.ExecuteReaderAsync();
while (await r.ReadAsync()) Console.WriteLine($"{r.GetValue(0)} | seats={r.GetValue(1)} | vt={r.GetValue(2)} | active={r.GetValue(3)}");
