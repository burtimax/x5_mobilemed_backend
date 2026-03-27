using System.Text;
using System.Text.Json;
using Application.Models.WeekRation;
using Application.Services.RppgScan;
using Application.Services.User;
using Application.Services.UserExcludeProducts;
using Application.Services.X5Products;
using Infrastructure.Db.App.Entities;
using Microsoft.Extensions.Logging;
using ModuleLLM.Configuration;
using ModuleLLM.Models.OpenRouter;
using ModuleLLM.Services;
using Shared.Extensions;

namespace Application.Services.WeekRation;

public sealed class WeekRationGeneratorService : IWeekRationGeneratorService
{
    private static readonly JsonSerializerOptions RationJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private const string SystemPrompt =
        """
        Ты профессиональный врач-диетолог. Составь рацион на 7 дней только из товаров каталога, переданного ниже.

        Требования к ответу:
        1. Верни только JSON без пояснений, markdown и дополнительного текста.
        2. Корневой элемент ответа — массив ровно из 28 объектов.
        3. Каждый объект обязан содержать поля:
           - day: число от 1 до 7
           - type: одно из значений "breakfast", "lunch", "snack", "dinner"
           - food: массив из 1-4 позиций в приеме пищи.
        4. Каждая комбинация (day, type) должна встречаться ровно один раз.
        5. Каждая позиция в массиве food обязана содержать:
           - id: id товара из каталога
           - weight: вес в граммах, целое положительное число
           - replace: массив от 1 до 5 объектов-замен
           - reason: краткое объяснение выбора позиции, почему товар был подобран в рацион, 1 предложение (до 10 слов)
        6. Каждый объект в replace обязан содержать:
           - id: id товара из каталога
           - weight: вес в граммах, целое положительное число
        7. Используй только товары из каталога. Не придумывай товары, названия, id и характеристики.
        8. Не включай товары, исключённые пользователем.
        9. При подборе рациона опирайся на:
           - расшифровку показателей здоровья и списки продуктов из неё (если блок передан в сообщении пользователя)
           - прочие данные скана (если переданы в сообщении пользователя)
           - целевую калорийность ({0} ккал) и ограничения из профиля
           - предпочтения и исключения пользователя
        10. Рацион должен быть разнообразным: не повторяй одни и те же позиции слишком часто без необходимости.
        11. Замены в replace должны быть реалистичными и близкими по роли в рационе.
        12. Если подходящих товаров мало, всё равно верни корректный JSON строго в указанной структуре.
        13. Если в сообщении пользователя есть расшифровка показателей (клиническое питание), обязательно учитывай её и разрешённые и нежелательные продукты из неё при выборе позиций только из каталога.

        Правила подбора:
        - breakfast — завтрак
        - lunch — обед
        - snack — перекус
        - dinner — ужин
        - Учитывай баланс рациона на день и на неделю.
        - Не используй несовместимые между собой товары.
        - По возможности распределяй калорийность между приёмами пищи адекватно типу meal.
        - СТРОГО Не используй товары, которые содержат продукты, исключенные пользователем.
        """;

    private const string HealthInterpretationSystemPrompt =
        """
        Ты — врач-диетолог и специалист по клиническому питанию, который даёт рекомендации в рамках доказательной медицины.
        Отвечай строго одним JSON-объектом по схеме ответа API, без markdown, без кодовых блоков и без текста вне JSON.
        Будь конкретен: называй продукты и практические приёмы; избегай общих фраз без пользы.
        """;

    private const string FocusBiomarkerKeyLegend =
        """
        Соответствие показателей (русское название → ключ в данных):
        - Гликированный гемоглобин (HbA1c) → hemoglobinA1c
        - Риск высокого HbA1c → highHemoglobinA1CRisk
        - Риск высокой глюкозы натощак → highFastingGlucoseRisk
        - Систолическое давление → bloodPressureSystolic
        - Диастолическое давление → bloodPressureDiastolic
        - Риск высокого давления → highBloodPressureRisk
        - Риск высокого общего холестерина → highTotalCholesterolRisk
        - Гемоглобин → hemoglobin
        - Риск низкого гемоглобина → lowHemoglobinRisk
        - 10-летний риск ASCVD → ascvdRisk
        - Возраст сердца → heartAge
        """;

    private const int HealthInterpretationMaxTokens = 8192;

    // private static readonly string WeekRationResponseFormatJson =
    //     """
    //     {
    //       "type": "json_schema",
    //       "json_schema": {
    //         "name": "week_ration",
    //         "strict": true,
    //         "schema": {
    //           "type": "array",
    //           "description": "Список приёмов пищи по дням недели. Каждый объект содержит номер дня, тип приёма пищи и список товаров. Для каждого товара указывается идентификатор товара, краткая причина выбора, вес порции в граммах и варианты замены.",
    //           "items": {
    //             "type": "object",
    //             "additionalProperties": false,
    //             "required": ["day", "type", "food"],
    //             "properties": {
    //               "day": {
    //                 "type": "integer",
    //                 "minimum": 1,
    //                 "maximum": 7,
    //                 "description": "Номер дня недели от 1 до 7."
    //               },
    //               "type": {
    //                 "type": "string",
    //                 "description": "Тип приёма пищи.",
    //                 "enum": ["breakfast", "lunch", "snack", "dinner"]
    //               },
    //               "food": {
    //                 "type": "array",
    //                 "description": "Список товаров для этого приёма пищи.",
    //                 "items": {
    //                   "type": "object",
    //                   "additionalProperties": false,
    //                   "required": ["id", "weight", "reason", "replace"],
    //                   "properties": {
    //                     "id": {
    //                       "type": "integer",
    //                       "description": "Идентификатор товара."
    //                     },
    //                     "reason": {
    //                       "type": "string",
    //                       "description": "Одно короткое предложение, почему именно этот товар нужен в рационе. Причина может быть связана с показателями здоровья или КБЖУ. В предложении не должно быть названия товара, кратко и лаконично (до 12 слов)."
    //                     },
    //                     "weight": {
    //                       "type": "integer",
    //                       "description": "Сколько грамм нужно съесть. Вес порции в граммах."
    //                     },
    //                     "replace": {
    //                       "type": "array",
    //                       "minItems": 1,
    //                       "maxItems": 5,
    //                       "description": "Список товаров, которыми можно заменить данный товар в этом приёме пищи. Нужно предложить от 1 до 5 вариантов.",
    //                       "items": {
    //                         "type": "object",
    //                         "additionalProperties": false,
    //                         "required": ["id", "weight"],
    //                         "properties": {
    //                           "id": {
    //                             "type": "integer",
    //                             "description": "Идентификатор товара-замены."
    //                           },
    //                           "weight": {
    //                             "type": "integer",
    //                             "description": "Вес порции товара-замены в граммах."
    //                           }
    //                         }
    //                       }
    //                     }
    //                   }
    //                 }
    //               }
    //             }
    //           }
    //         }
    //       }
    //     }
    //     """;


    // ANTHROPIC CLAUDE
    private static readonly string WeekRationResponseFormatJson =
        """
        {
          "type": "json_schema",
          "json_schema": {
            "name": "week_ration",
            "strict": true,
            "schema": {
              "type": "array",
              "description": "Список приёмов пищи по дням недели. Каждый объект содержит номер дня, тип приёма пищи и список товаров. Для каждого товара указывается идентификатор товара, краткая причина выбора, вес порции в граммах и варианты замены.",
              "items": {
                "type": "object",
                "additionalProperties": false,
                "required": ["day", "type", "food"],
                "properties": {
                  "day": {
                    "type": "integer",
                    "description": "Номер дня недели от 1 до 7."
                  },
                  "type": {
                    "type": "string",
                    "description": "Тип приёма пищи.",
                    "enum": ["breakfast", "lunch", "snack", "dinner"]
                  },
                  "food": {
                    "type": "array",
                    "description": "Список товаров для этого приёма пищи.",
                    "items": {
                      "type": "object",
                      "additionalProperties": false,
                      "required": ["id", "weight", "reason", "replace"],
                      "properties": {
                        "id": {
                          "type": "integer",
                          "description": "Идентификатор товара."
                        },
                        "reason": {
                          "type": "string",
                          "description": "Одно короткое предложение, почему именно этот товар нужен в рационе. Причина может быть связана с показателями здоровья или КБЖУ. В предложении не должно быть названия товара, кратко и лаконично (до 10 слов)."
                        },
                        "weight": {
                          "type": "integer",
                          "description": "Сколько грамм нужно съесть. Вес порции в граммах."
                        },
                        "replace": {
                          "type": "array",
                          "description": "Список товаров, которыми можно заменить данный товар в этом приёме пищи. Нужно предложить от 1 до 5 вариантов.",
                          "items": {
                            "type": "object",
                            "additionalProperties": false,
                            "required": ["id", "weight"],
                            "properties": {
                              "id": {
                                "type": "integer",
                                "description": "Идентификатор товара-замены."
                              },
                              "weight": {
                                "type": "integer",
                                "description": "Вес порции товара-замены в граммах."
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
        """;

    private static readonly string[] WeekRationMealTypes = ["breakfast", "lunch", "snack", "dinner"];

    private readonly ILlmApiService _llmApi;
    private readonly OpenRouterApiConfiguration _openRouterConfig;
    private readonly IRppgScanReportService _scanReportService;
    private readonly IX5ProductsService _productsService;
    private readonly IUserExcludeProductsService _excludeProductsService;
    private readonly IUserService _userService;
    private readonly ILogger<WeekRationGeneratorService> _logger;

    public WeekRationGeneratorService(
        ILlmApiService llmApi,
        OpenRouterApiConfiguration openRouterConfig,
        IRppgScanReportService scanReportService,
        IX5ProductsService productsService,
        IUserExcludeProductsService excludeProductsService,
        IUserService userService,
        ILogger<WeekRationGeneratorService> logger)
    {
        _llmApi = llmApi;
        _openRouterConfig = openRouterConfig;
        _scanReportService = scanReportService;
        _productsService = productsService;
        _excludeProductsService = excludeProductsService;
        _userService = userService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<WeekRationGeneratorOutcome> GenerateAsync(
        WeekRationRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var rationContext = await _scanReportService.GetRationLlmContextForUserAsync(
            request.ScanId,
            userId,
            WeekRationFocusBiomarkerKeys.All,
            cancellationToken);
        if (rationContext == null)
        {
            return new WeekRationGeneratorOutcome(
                404,
                new WeekRationResponseDto
                {
                    Error = "Скан не найден или не принадлежит текущему пользователю."
                });
        }

        HealthMetricsInterpretationDto? interpretation = null;
        if (!string.IsNullOrWhiteSpace(rationContext.FocusMetricsReportText))
        {
            var (interpretationData, interpretationFailure) = await InterpretHealthMetricsAsync(
                rationContext.FocusMetricsReportText,
                request,
                cancellationToken);
            if (interpretationFailure != null)
                return interpretationFailure;
            interpretation = interpretationData;
        }

        string scanBlocksForRation;
        if (interpretation != null)
        {
            scanBlocksForRation =
                "# Расшифровка показателей здоровья (клиническое питание)\n"
                + FormatInterpretationForRationPrompt(interpretation);
            /*+ "\n\n### Прочие данные скана (сырые значения фокусных показателей исключены)\n"
            + rationContext.SupplementaryReportText;*/
        }
        else
        {
            scanBlocksForRation = "### Отчёт по скану\n" + rationContext.SupplementaryReportText;
        }

        var user = await _userService.GetByIdAsync(userId);
        var profile = user?.Profile;
        var maintenanceKcal = DailyEnergyEstimate.EstimateMaintenanceKcal(
            profile?.Age,
            profile?.Gender,
            profile?.Weight,
            profile?.Height);

        var profileBlock = FormatProfileBlock(profile, maintenanceKcal);

        var excluded = await _excludeProductsService.GetUserExcludeProductsAsync(userId, cancellationToken);
        var excludedBlock = excluded.Count == 0
            ? "(исключений нет)"
            : string.Join("\n", excluded.Select(e => "- " + e));

        var catalogText = await _productsService.GetProductsCatalogTextAsync(cancellationToken);

        var userMessage =
            "# Профиль и калорийность\n"
            + profileBlock
            + "\n\n" + scanBlocksForRation
            + "\n\n## Исключённые для пользователя продукты / категории\n"
            + excludedBlock
            + "\n\n### Каталог товаров (используй только ID из карточек)\n"
            + catalogText
            + $"\n\nСоставь недельный рацион: корневой JSON-массив из 28 элементов (каждый: day, type, food) по схеме. Учти исключающие товары. Целевая калорийность - ({maintenanceKcal}) ккал в день.";

        var model = string.IsNullOrWhiteSpace(request.Model) ? _openRouterConfig.Model : request.Model.Trim();

        string prompt = SystemPrompt.F(maintenanceKcal);

        var chatRequest = new OpenRouterChatRequest
        {
            Model = model,
            Stream = false,
            Temperature = request.Temperature ?? 0.35,
            MaxTokens = request.MaxTokens ?? 16384,
            TopP = request.TopP,
            ResponseFormatJson = WeekRationResponseFormatJson,
            Messages =
            [
                new OpenRouterMessage { Role = "system", Content = prompt },
                new OpenRouterMessage { Role = "user", Content = userMessage }
            ]
        };

        var result = await _llmApi.SendChatCompletionAsync(chatRequest, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return new WeekRationGeneratorOutcome(
                502,
                new WeekRationResponseDto { Error = result.Error ?? "Ошибка OpenRouter." });
        }

        var choices = result.Value.Choices;
        if (choices == null || choices.Count == 0 ||
            choices[0].Message is not { } message ||
            string.IsNullOrWhiteSpace(message.Content))
        {
            _logger.LogError("Модель LLM вернула пустой ответ");
            return new WeekRationGeneratorOutcome(
                502,
                new WeekRationResponseDto { Error = "Модель вернула пустой ответ." });
        }

        List<DayRationMealSlotDto>? slots;
        try
        {
            var jsonText = UnwrapAssistantJsonPayload(message.Content);
            slots = JsonSerializer.Deserialize<List<DayRationMealSlotDto>>(jsonText, RationJsonOptions);
        }
        catch (JsonException e)
        {
            _logger.LogError(e, "Ошибка парсинга JSON рациона: ");
            return new WeekRationGeneratorOutcome(
                502,
                new WeekRationResponseDto
                {
                    Error = "Не удалось разобрать JSON ответа модели.",
                    RawAssistantContent = message.Content
                });
        }

        if (slots is null)
        {
            return new WeekRationGeneratorOutcome(
                502,
                new WeekRationResponseDto
                {
                    Error = "Ответ модели не удалось разобрать как список приёмов пищи.",
                    RawAssistantContent = message.Content
                });
        }

        if (!IsValidWeekRationSlots(slots, out var slotValidationError))
        {
            return new WeekRationGeneratorOutcome(
                502,
                new WeekRationResponseDto
                {
                    Error = slotValidationError,
                    RawAssistantContent = message.Content
                });
        }

        var productIds = WeekRationEnrichment.CollectProductIds(slots);
        var productsById = await _productsService.GetProductsByIdsAsync(productIds, cancellationToken);
        WeekRationEnrichment.AttachProducts(slots, productsById);

        return new WeekRationGeneratorOutcome(200, new WeekRationResponseDto { Ration = slots });
    }

    private async Task<(HealthMetricsInterpretationDto? Data, WeekRationGeneratorOutcome? Error)> InterpretHealthMetricsAsync(
        string focusMetricsReportText,
        WeekRationRequest request,
        CancellationToken cancellationToken)
    {
        var userContent =
            "На основе приведённых ниже показателей здоровья (только они — источник чисел и зон) составь персональные рекомендации по питанию.\n\n"
            + "1) Кратко объясни, что означают эти показатели и какие цели питания при таких рисках.\n"
            + "2) Дай практические рекомендации по питанию простым и понятным языком.\n"
            + "3) Составь расширенный список разрешённых продуктов.\n"
            + "4) Дай подробный список нежелательных и запрещённых продуктов.\n\n"
            + "Не давай общих фраз без пользы — нужны конкретные продукты и практические советы.\n\n"
            + FocusBiomarkerKeyLegend
            + "\n\n### Данные показателей (фрагмент отчёта скана)\n"
            + focusMetricsReportText;

        var model = string.IsNullOrWhiteSpace(request.Model) ? _openRouterConfig.Model : request.Model.Trim();

        var chatRequest = new OpenRouterChatRequest
        {
            Model = model,
            Stream = false,
            Temperature = request.Temperature ?? 0.35,
            MaxTokens = HealthInterpretationMaxTokens,
            TopP = request.TopP,
            ResponseFormatJson = HealthMetricsInterpretationJsonSchema.ResponseFormatJson,
            Messages =
            [
                new OpenRouterMessage { Role = "system", Content = HealthInterpretationSystemPrompt },
                new OpenRouterMessage { Role = "user", Content = userContent }
            ]
        };

        var result = await _llmApi.SendChatCompletionAsync(chatRequest, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return (null, new WeekRationGeneratorOutcome(
                502,
                new WeekRationResponseDto
                {
                    Error = result.Error ?? "Ошибка OpenRouter (расшифровка показателей)."
                }));
        }

        var choices = result.Value.Choices;
        if (choices == null || choices.Count == 0 ||
            choices[0].Message is not { } message ||
            string.IsNullOrWhiteSpace(message.Content))
        {
            _logger.LogError("Модель LLM вернула пустой ответ при расшифровке показателей");
            return (null, new WeekRationGeneratorOutcome(
                502,
                new WeekRationResponseDto { Error = "Модель вернула пустой ответ при расшифровке показателей." }));
        }

        HealthMetricsInterpretationDto? dto;
        try
        {
            var jsonText = UnwrapAssistantJsonPayload(message.Content);
            dto = JsonSerializer.Deserialize<HealthMetricsInterpretationDto>(jsonText, RationJsonOptions);
        }
        catch (JsonException e)
        {
            _logger.LogError(e, "Ошибка парсинга JSON расшифровки показателей");
            return (null, new WeekRationGeneratorOutcome(
                502,
                new WeekRationResponseDto
                {
                    Error = "Не удалось разобрать JSON расшифровки показателей.",
                    RawAssistantContent = message.Content
                }));
        }

        if (dto == null)
        {
            return (null, new WeekRationGeneratorOutcome(
                502,
                new WeekRationResponseDto
                {
                    Error = "Ответ расшифровки показателей пуст.",
                    RawAssistantContent = message.Content
                }));
        }

        return (dto, null);
    }

    private static string FormatInterpretationForRationPrompt(HealthMetricsInterpretationDto dto)
    {
        var sb = new StringBuilder();
        sb.AppendLine("##### 1) Значение показателей и цели питания");
        sb.AppendLine(dto.IndicatorMeaningsAndNutritionGoals.Trim());
        sb.AppendLine();
        sb.AppendLine("##### 2) Практические рекомендации по питанию");
        sb.AppendLine(dto.PracticalNutritionRecommendations.Trim());
        sb.AppendLine();
        sb.AppendLine("##### 3) Расширенный список разрешённых продуктов");
        foreach (var line in dto.AllowedProductsExtended)
            sb.AppendLine("- " + line.Trim());
        sb.AppendLine();
        sb.AppendLine("##### 4) Нежелательные и запрещённые продукты");
        foreach (var line in dto.UndesirableAndForbiddenProducts)
            sb.AppendLine("- " + line.Trim());
        return sb.ToString().TrimEnd();
    }

    private static string FormatProfileBlock(UserProfileEntity? profile, int? maintenanceKcal)
    {
        if (profile == null)
            return "Профиль не заполнен — ориентируйся на данные в отчёте скана; точный калораж по формуле недоступен.";

        var lines = new List<string>
        {
            $"Возраст (лет): {profile.Age?.ToString() ?? "не указан"}",
            $"Рост (см): {profile.Height?.ToString() ?? "не указан"}",
            $"Вес (кг): {profile.Weight?.ToString() ?? "не указан"}",
            $"Пол: {FormatGender(profile.Gender)}"
        };

        if (maintenanceKcal.HasValue)
            lines.Add(
                $"Ориентир суточной энергозатраты (лёгкая активность, Mifflin–St Jeor ×1,375): около {maintenanceKcal} ккал/сутки.");
        else
            lines.Add("Недостаточно данных для формулы калоража — ориентируйся на отчёт и здравый смысл.");

        return string.Join("\n", lines);
    }

    private static string FormatGender(Gender? g) => g switch
    {
        Gender.Male => "мужской",
        Gender.Female => "женский",
        _ => "не указан"
    };

    private static bool IsValidWeekRationSlots(IReadOnlyList<DayRationMealSlotDto> slots, out string errorMessage)
    {
        if (slots.Count != 28)
        {
            errorMessage = "Ожидается 28 записей приёма пищи (7 дней × 4 типа: breakfast, lunch, snack, dinner).";
            return false;
        }

        var seen = new HashSet<(int Day, string Type)>();
        foreach (var s in slots)
        {
            if (s.Day is < 1 or > 7)
            {
                errorMessage = "Поле day должно быть от 1 до 7.";
                return false;
            }

            var t = (s.Type ?? string.Empty).Trim().ToLowerInvariant();
            if (Array.IndexOf(WeekRationMealTypes, t) < 0)
            {
                errorMessage =
                    "Поле type должно быть одним из: breakfast, lunch, snack, dinner.";
                return false;
            }

            if (!seen.Add((s.Day, t)))
            {
                errorMessage = "Повтор сочетания day и type.";
                return false;
            }
        }

        for (var d = 1; d <= 7; d++)
        {
            foreach (var t in WeekRationMealTypes)
            {
                if (!seen.Contains((d, t)))
                {
                    errorMessage = "Не хватает приёма пищи для полной недели (каждая пара день + тип должна быть ровно один раз).";
                    return false;
                }
            }
        }

        errorMessage = string.Empty;
        return true;
    }

    private static string UnwrapAssistantJsonPayload(string content)
    {
        var t = content.Trim();
        if (!t.StartsWith("```", StringComparison.Ordinal))
            return t;

        var firstLineBreak = t.IndexOf('\n');
        if (firstLineBreak < 0)
            return t;

        t = t[(firstLineBreak + 1)..];
        var fence = t.LastIndexOf("```", StringComparison.Ordinal);
        if (fence >= 0)
            t = t[..fence];

        return t.Trim();
    }
}
