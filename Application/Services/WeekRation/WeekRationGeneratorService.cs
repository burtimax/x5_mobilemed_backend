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
        8. Исключения пользователя — **обязательное** условие безопасности и предпочтений: в рационе не должно быть ни одного товара (ни в food, ни в replace), который нарушает список исключений из сообщения пользователя. Пропуск исключений недопустим.
        9. При подборе рациона опирайся на:
           - расшифровку показателей здоровья и списки продуктов из неё (если блок передан в сообщении пользователя)
           - прочие данные скана (если переданы в сообщении пользователя)
           - целевую калорийность: {0} и ограничения из профиля
           - предпочтения и исключения пользователя
        10. Рацион должен быть разнообразным: не повторяй одни и те же позиции слишком часто без необходимости.
        11. Замены в replace должны быть реалистичными и близкими по роли в рационе.
        12. Если подходящих товаров мало, всё равно верни корректный JSON строго в указанной структуре.
        13. Если в сообщении пользователя есть расшифровка показателей (клиническое питание), обязательно учитывай её и разрешённые и нежелательные продукты из неё при выборе позиций только из каталога.
        14. В сообщении пользователя заданы ориентиры по калориям на день и по каждому приёму пищи (в ккал, если удалось вычислить суточную цель). Соблюдай эти ориентиры при подборе весов порций; небольшие отклонения допустимы (порядка нескольких процентов или десятков килокалорий на приём), если суточный баланс в целом сохраняется.
        15. Перед финальным ответом **проверь состав** каждого выбранного товара и каждой замены (replace) по тексту карточки в каталоге: ингредиенты, наименование и описание не должны содержать исключённые пользователем продукты и **их производные** (например, при исключении молока — недопустимы товары, где в составе есть молоко, сливки, сухое молоко; при исключении арахиса — паста и продукты с арахисом в составе, если это видно из карточки). Если товар сомнителен — выбери другой id из каталога без нарушений.

        Правила подбора:
        - breakfast — завтрак
        - lunch — обед
        - snack — перекус
        - dinner — ужин
        - Учитывай баланс рациона на день и на неделю.
        - Не используй несовместимые между собой товары.
        - Ориентируйся на распределение калорий по приёмам из сообщения пользователя (абсолютные значения в ккал, где указаны).
        - Соблюдай исключения пользователя буквально и по смыслу состава: не включай запрещённые ингредиенты и очевидные производные, проверяя карточки товаров в каталоге.
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
        var calorieAndDistributionBlock = FormatDailyCalorieAndDistributionBlock(maintenanceKcal);

        var excluded = await _excludeProductsService.GetUserExcludeProductsAsync(userId, cancellationToken);
        var excludedLines = excluded.Count == 0
            ? "(исключений нет — список пуст.)"
            : string.Join("\n", excluded.Select(e => "- " + e));
        var excludedSection =
            "## Исключённые для пользователя продукты / категории\n"
            + (excluded.Count == 0
                ? "Пользователь не задал исключений. Всё равно не подбирай товары с заведомо непереносимыми ингредиентами, если это явно следует из профиля или расшифровки показателей.\n"
                : """
                **Критическое требование:** ниже перечислено то, что пользователь **не употребляет** (аллергии, непереносимость, врачебные или личные ограничения). Эти продукты и категории **не должны** попадать в рацион **ни в основных позициях (food), ни в заменах (replace)**. Это важнее удобства подбора и разнообразия.

                **Проверка состава:** для **каждого** выбранного id из каталога прочитай карточку товара (название, описание, состав) и убедись, что там **нет** исключённого продукта и **нет производных** того же сырья (например, при исключении «молоко» отвергай йогурт/сыр/сливки, если в составе указано молоко или молочный белок; при исключении «глютен» — товары с пшеницей/ячменём/ржью в составе, если это видно из карточки). При сомнениях выбери **другой** товар из каталога.

                Список исключений:

                """.ReplaceLineEndings("\n"))
            + excludedLines;

        var catalogText = await _productsService.GetProductsCatalogTextAsync(cancellationToken);

        var userMessage =
            "# Профиль и калорийность\n"
            + profileBlock
            + "\n\n" + calorieAndDistributionBlock
            + "\n\n" + scanBlocksForRation
            + "\n\n" + excludedSection
            + "\n\n### Каталог товаров (используй только ID из карточек)\n"
            + catalogText
            + "\n\nСоставь недельный рацион: корневой JSON-массив из 28 элементов (каждый: day, type, food) по схеме. Строго соблюдай список исключений и проверку состава по карточкам; учти ориентиры по калориям из блока выше.";

        var model = string.IsNullOrWhiteSpace(request.Model) ? _openRouterConfig.Model : request.Model.Trim();

        string prompt = SystemPrompt.F(FormatSystemCaloriePhrase(maintenanceKcal));

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

    private static string FormatSystemCaloriePhrase(int? maintenanceKcal) =>
        maintenanceKcal.HasValue
            ? $"{maintenanceKcal.Value} ккал/сутки (ориентир поддержания при лёгкой активности, Mifflin–St Jeor ×1,375)"
            : "см. блок «Целевой калораж и распределение по приёмам пищи» в сообщении пользователя; при отсутствии числа оцени разумный суточный ориентир из контекста";

    private static string FormatDailyCalorieAndDistributionBlock(int? maintenanceKcal)
    {
        if (maintenanceKcal is { } d)
        {
            var bLo = (int)Math.Round(0.25 * d);
            var bHi = (int)Math.Round(0.30 * d);
            var lLo = (int)Math.Round(0.30 * d);
            var lHi = (int)Math.Round(0.35 * d);
            var sLo = (int)Math.Round(0.10 * d);
            var sHi = (int)Math.Round(0.15 * d);
            var dLo = (int)Math.Round(0.20 * d);
            var dHi = (int)Math.Round(0.25 * d);

            return $"""
            ## Целевой калораж и распределение по приёмам пищи

            Целевая суточная калорийность для подбора рациона: **{d} ккал/день** (ориентир поддержания массы при лёгкой активности по профилю).

            Ориентиры по калориям **на один типичный день** (в килокалориях на приём; **незначительные отклонения допустимы** — порядка нескольких процентов или до примерно 50–100 ккал на приём, если дневной суммарный калораж остаётся близок к цели):

            - **Завтрак (breakfast)**: ориентир **{bLo}–{bHi} ккал**. Помогает запустить метаболизм и дать энергию на начало дня.
            - **Обед (lunch)**: ориентир **{lLo}–{lHi} ккал**. Самый обильный приём пищи, обеспечивает энергией на вторую половину дня.
            - **Перекус (snack)**: ориентир **{sLo}–{sHi} ккал**. Снижает риск сильного голода перед ужином.
            - **Ужин (dinner)**: ориентир **{dLo}–{dHi} ккал**. Относительно лёгкий, но питательный приём пищи.

            Подбирай веса порций в JSON так, чтобы калорийность каждого приёма (по данным каталога) попадала в указанные коридоры; структура повторяется на все 7 дней с допустимыми малыми колебаниями между днями.
            """;
        }

        return """
            ## Целевой калораж и распределение по приёмам пищи

            Точную суточную целевую калорийность по формуле из профиля оценить нельзя (недостаточно данных). Определи разумный суточный ориентир **в килокалориях** из контекста и разложи его на приёмы **в абсолютных ккал**; **незначительные отклонения допустимы**.

            Ориентирные **доли суточного калоража** (пересчитай их в ккал, как только зафиксировано число ккал/день):

            - **Завтрак (breakfast)**: **25–30%** — запуск метаболизма и энергия на начало дня.
            - **Обед (lunch)**: **30–35%** — самый обильный приём, энергия на вторую половину дня.
            - **Перекус (snack)**: **10–15%** — чтобы избежать сильного голода перед ужином.
            - **Ужин (dinner)**: **20–25%** — лёгкий, но питательный приём.

            В ответе JSON выбирай веса так, чтобы по калориям каждый приём соответствовал пересчитанным коридорам.
            """;
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
