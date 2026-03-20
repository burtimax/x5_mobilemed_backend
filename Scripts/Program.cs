using Scripts.CategoriesAndProductsToJson;
using Scripts.CategoriesAndProductsToText;

// JSON: dotnet run --project Scripts [строка_подключения] [путь_к_output_json]
// Текст: dotnet run --project Scripts --to-text [строка_подключения] [путь_к_output_txt]
string? connectionString;
string? outputPath;

connectionString = args.Length > 1 ? args[1] : null;
outputPath = args.Length > 2 ? args[2] : null;
await CategoriesAndProductsToText.RunAsync(connectionString, outputPath);

// if (args.Length > 0 && string.Equals(args[0], "--to-text", StringComparison.OrdinalIgnoreCase))
// {
//
// }
// else
// {
//     connectionString = args.Length > 0 ? args[0] : null;
//     outputPath = args.Length > 1 ? args[1] : null;
//     await CategoriesAndProductsToJson.RunAsync(connectionString, outputPath);
// }
