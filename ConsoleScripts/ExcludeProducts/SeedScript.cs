// using Microsoft.EntityFrameworkCore;
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;
// using Infrastructure.Db.App;
// using Infrastructure.Db.App.Entities;
//
// string dbConnection = null; //"Host=127.0.0.1;Port=5432;Database=x5_mobilemed_db;Username=postgres;Password=123;Include Error Detail=true";
// DbContextOptionsBuilder<AppDbContext> optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
// optionsBuilder.UseNpgsql(dbConnection);
// AppDbContext db = new AppDbContext(optionsBuilder.Options);
// await SeedAsync(db);
//
//
//      static async Task SeedAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
//     {
//         var products = new[]
//         {
//             "молоко",
//             "молочный белок",
//             "творог",
//             "сыр",
//             "йогурт",
//             "кефир",
//             "сливки",
//             "сметана",
//             "масло сливочное",
//             "мороженое",
//             "продукты с лактозой",
//
//             "яйца",
//             "белок яйца",
//             "желток яйца",
//             "перепелиные яйца",
//             "яичный порошок",
//
//             "глютен",
//             "пшеница",
//             "рожь",
//             "ячмень",
//             "овес",
//             "мука пшеничная",
//             "мучное",
//             "хлеб",
//             "булочки",
//             "макароны",
//             "манка",
//             "кус-кус",
//             "булгур",
//             "спельта",
//
//             "рис",
//             "гречка",
//             "кукуруза",
//             "пшено",
//             "перловка",
//             "овсянка",
//
//             "соя",
//             "арахис",
//             "фасоль",
//             "горох",
//             "чечевица",
//             "нут",
//             "маш",
//             "бобовые",
//
//             "орехи",
//             "миндаль",
//             "фундук",
//             "грецкий орех",
//             "кешью",
//             "фисташки",
//             "пекан",
//             "бразильский орех",
//             "кедровые орехи",
//             "кунжут",
//             "семена подсолнечника",
//             "семена тыквы",
//             "лен",
//             "чиа",
//
//             "рыба",
//             "красная рыба",
//             "белая рыба",
//             "тунец",
//             "лосось",
//             "треска",
//             "сельдь",
//             "морепродукты",
//             "моллюски",
//             "ракообразные",
//             "креветки",
//             "крабы",
//             "раки",
//             "омары",
//             "лобстеры",
//             "мидии",
//             "устрицы",
//             "кальмары",
//             "осьминоги",
//
//             "мясо любое",
//             "мясо красное",
//             "свинина",
//             "говядина",
//             "телятина",
//             "баранина",
//             "курица",
//             "индейка",
//             "утка",
//             "субпродукты",
//             "печень",
//             "сердце",
//             "полуфабрикаты",
//
//             "картофель",
//             "помидоры",
//             "свекла",
//             "перец",
//             "баклажаны",
//             "сельдерей",
//             "лук",
//             "чеснок",
//             "морковь",
//             "капуста",
//             "огурцы",
//             "кабачки",
//             "тыква",
//             "редис",
//             "редька",
//             "шпинат",
//             "щавель",
//             "грибы",
//
//             "пасленовые",
//
//             "цитрусовые",
//             "апельсин",
//             "мандарин",
//             "лимон",
//             "грейпфрут",
//             "лайм",
//             "клубника",
//             "земляника",
//             "малина",
//             "вишня",
//             "черешня",
//             "виноград",
//             "изюм",
//             "яблоки",
//             "груши",
//             "персики",
//             "абрикосы",
//             "киви",
//             "бананы",
//             "ананас",
//             "гранат",
//             "хурма",
//
//             "курага",
//             "чернослив",
//             "инжир сушеный",
//             "финики",
//             "мед",
//             "прополис",
//             "пыльца",
//
//             "дрожжи",
//             "хлеб на дрожжах",
//             "квас",
//             "пиво",
//
//             "аспартам",
//             "диоксид серы",
//             "сульфиты",
//             "добавленный сахар",
//             "красители",
//             "консерванты",
//             "ароматизаторы",
//             "усилители вкуса",
//
//             "супы",
//             "колбасы",
//             "сосиски",
//             "паштеты",
//             "соусы",
//             "сладости",
//             "шоколад",
//             "кондитерские изделия"
//         };
//
//         var normalizedProducts = products
//             .Select(p => p.Trim())
//             .Where(p => !string.IsNullOrWhiteSpace(p))
//             .Distinct(StringComparer.OrdinalIgnoreCase)
//             .ToList();
//
//         var existingNames = await dbContext.Set<ExcludeProductEntity>()
//             .Select(x => x.ProductName)
//             .ToListAsync(cancellationToken);
//
//         var existingSet = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);
//
//         var entitiesToAdd = normalizedProducts
//             .Where(product => !existingSet.Contains(product))
//             .Select(product => new ExcludeProductEntity
//             {
//                 Id = Guid.CreateVersion7(),
//                 ProductName = product
//             })
//             .ToList();
//
//         if (entitiesToAdd.Count == 0)
//             return;
//
//         await dbContext.Set<ExcludeProductEntity>().AddRangeAsync(entitiesToAdd, cancellationToken);
//         await dbContext.SaveChangesAsync(cancellationToken);
//     }
//
