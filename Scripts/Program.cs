using Scripts.CategoriesAndProductsToText;
using Scripts.DbTableProductsToExcel;

// Экспорт prods3 в Excel (с фото, первый URL из images):
//   dotnet run --project Scripts --to-excel [строка_подключения] [путь_к_output.xlsx]
//
// Текст (категории и товары), явный режим:
//   dotnet run --project Scripts --to-text [строка_подключения] [путь_к_output_txt]
//
// Текст без флага (как раньше: первый аргумент не используется, conn = args[1]):
//   dotnet run --project Scripts _ [строка_подключения] [путь_к_output_txt]

await TestGeminiScript.RunAsync();

Console.ReadLine();
