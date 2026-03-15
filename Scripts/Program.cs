using Scripts.DbMigrateProductsAndCategories;

// Поддержка строки подключения к целевой БД через аргумент:
// dotnet run -- "Host=...;Database=..."
var targetConn = args.Length > 0 ? args[0] : null;

await MigrateProductsAndCategories.RunAsync(targetConn);
