using Scripts.CategoriesAndProductsToJson;
using Scripts.CategoriesAndProductsToText;
using Scripts.DbMigrateProductsAndCategories;
using Scripts.DbTableProductsToExcel;
using Scripts.DownloadImagesToFolder;

//await TestGeminiScript.RunAsync();

// await CategoriesAndProductsToText.RunAsync();

//await CategoriesAndProductsToJson.RunAsync();

await DownloadImagesToFolder.RunAsync(targetDirectory: @"C:\Users\timof\Desktop\генерация рациона\картинки");

//await MigrateProductsAndCategories.RunAsync();

Console.ReadLine();
