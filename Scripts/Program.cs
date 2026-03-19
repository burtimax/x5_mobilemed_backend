using Scripts.CategoriesAndProductsToJson;

// dotnet run --project Scripts [строка_подключения] [путь_к_output_json]
var connectionString = args.Length > 0 ? args[0] : null;
var outputPath = args.Length > 1 ? args[1] : null;

await CategoriesAndProductsToJson.RunAsync(connectionString, outputPath);
