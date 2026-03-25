using System.Text.Json;
using Api.Extensions;
using Application.Models.WeekRation;
using Application.Services.RppgScan;
using Application.Services.User;
using Application.Services.UserExcludeProducts;
using Application.Services.X5Products;
using FastEndpoints;
using Infrastructure.Db.App.Entities;
using ModuleLLM.Configuration;
using ModuleLLM.Models.OpenRouter;
using ModuleLLM.Services;

namespace Api.Endpoints.App.OpenRouterChat;

/// <summary>
/// Недельный рацион по результатам RPPG-скана: отчёт скана, каталог X5, исключения пользователя, профиль (калораж).
/// </summary>
public sealed class OpenRouterWeekRationEndpoint : Endpoint<WeekRationRequest, WeekRationResponseDto>
{
    private static readonly JsonSerializerOptions RationJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private const string SystemPrompt =
        "Ты врач-диетолог и составляешь рацион на 7 дней только из товаров каталога ниже. "
        + "Корневой ответ — массив из 28 объектов: у каждого day (1–7), type (breakfast|lunch|snack|dinner) и food — список позиций с id из каталога, по желанию reason, обязательно weigth (граммы) и replace (1–5 объектов с id и weigth). "
        + "Каждая пара (day, type) встречается ровно один раз. "
        + "Опирайся на отчёт сканирования и калорийность из профиля; не включай исключённые пользователем товары.";

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
                    "minimum": 1,
                    "maximum": 7,
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
                      "required": ["id", "weigth", "replace"],
                      "properties": {
                        "id": {
                          "type": "integer",
                          "description": "Идентификатор товара."
                        },
                        "reason": {
                          "type": "string",
                          "description": "Одно короткое предложение, почему именно этот товар нужен в рационе. Причина может быть связана с показателями здоровья или КБЖУ. В предложении не должно быть названия товара, кратно и лаконично (до 12 слов)."
                        },
                        "weigth": {
                          "type": "integer",
                          "description": "Сколько грамм нужно съесть. Вес порции в граммах."
                        },
                        "replace": {
                          "type": "array",
                          "minItems": 1,
                          "maxItems": 5,
                          "description": "Список товаров, которыми можно заменить данный товар в этом приёме пищи. Нужно предложить от 1 до 5 вариантов.",
                          "items": {
                            "type": "object",
                            "additionalProperties": false,
                            "required": ["id", "weigth"],
                            "properties": {
                              "id": {
                                "type": "integer",
                                "description": "Идентификатор товара-замены."
                              },
                              "weigth": {
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

    private readonly ILlmApiService _llmApi;
    private readonly OpenRouterApiConfiguration _openRouterConfig;
    private readonly IRppgScanReportService _scanReportService;
    private readonly IX5ProductsService _productsService;
    private readonly IUserExcludeProductsService _excludeProductsService;
    private readonly IUserService _userService;

    public OpenRouterWeekRationEndpoint(
        ILlmApiService llmApi,
        OpenRouterApiConfiguration openRouterConfig,
        IRppgScanReportService scanReportService,
        IX5ProductsService productsService,
        IUserExcludeProductsService excludeProductsService,
        IUserService userService)
    {
        _llmApi = llmApi;
        _openRouterConfig = openRouterConfig;
        _scanReportService = scanReportService;
        _productsService = productsService;
        _excludeProductsService = excludeProductsService;
        _userService = userService;
    }

    public override void Configure()
    {
        Post("openrouter/week-ration");
        Group<AppGroupEndpoints>();
        Summary(s =>
        {
            s.Summary = "Рацион на неделю по скану RPPG (OpenRouter)";
            s.Description =
                "По ID скана строит текст отчёта, подмешивает каталог товаров X5 и исключения пользователя; возвращает JSON рациона на 7 дней.";
        });
    }

    public override async Task HandleAsync(WeekRationRequest req, CancellationToken ct)
    {
        var userId = HttpContext.TokenData().UserId;

        var reportText = await _scanReportService.GetReportTextForUserAsync(req.ScanId, userId, ct);
        if (reportText == null)
        {
            await SendAsync(
                new WeekRationResponseDto { Error = "Скан не найден или не принадлежит текущему пользователю." },
                statusCode: 404,
                cancellation: ct);
            return;
        }

        var user = await _userService.GetByIdAsync(userId);
        var profile = user?.Profile;
        var maintenanceKcal = DailyEnergyEstimate.EstimateMaintenanceKcal(
            profile?.Age,
            profile?.Gender,
            profile?.Weight,
            profile?.Height);

        var profileBlock = FormatProfileBlock(profile, maintenanceKcal);

        var excluded = await _excludeProductsService.GetUserExcludeProductsAsync(userId, ct);
        var excludedBlock = excluded.Count == 0
            ? "(исключений нет)"
            : string.Join("\n", excluded.Select(e => "- " + e));

        var catalogText = await _productsService.GetProductsCatalogTextAsync(ct);

        var userMessage =
            "### Отчёт по скану\n"
            + reportText
            + "\n\n### Профиль и калорийность\n"
            + profileBlock
            + "\n\n### Исключённые для пользователя продукты / категории\n"
            + excludedBlock
            + "\n\n### Каталог товаров (используй только ID из карточек)\n"
            + catalogText
            + "\n\nСоставь недельный рацион: корневой JSON-массив из 28 элементов (каждый: day, type, food) по схеме.";

        var model = string.IsNullOrWhiteSpace(req.Model) ? _openRouterConfig.Model : req.Model.Trim();

        var chatRequest = new OpenRouterChatRequest
        {
            Model = model,
            Stream = false,
            Temperature = req.Temperature ?? 0.35,
            MaxTokens = req.MaxTokens ?? 16384,
            TopP = req.TopP,
            ResponseFormatJson = WeekRationResponseFormatJson,
            Messages =
            [
                new OpenRouterMessage { Role = "system", Content = SystemPrompt },
                new OpenRouterMessage { Role = "user", Content = userMessage }
            ]
        };

        var result = await _llmApi.SendChatCompletionAsync(chatRequest, ct);

        if (!result.IsSuccess || result.Value is null)
        {
            await SendAsync(
                new WeekRationResponseDto { Error = result.Error ?? "Ошибка OpenRouter." },
                statusCode: 502,
                cancellation: ct);
            return;
        }

        var choices = result.Value.Choices;
        if (choices == null || choices.Count == 0 ||
            choices[0].Message is not { } message ||
            string.IsNullOrWhiteSpace(message.Content))
        {
            await SendAsync(
                new WeekRationResponseDto { Error = "Модель вернула пустой ответ." },
                statusCode: 502,
                cancellation: ct);
            return;
        }

        List<WeekRationMealSlotDto>? slots;
        try
        {
            var jsonText = UnwrapAssistantJsonPayload(message.Content);
            slots = JsonSerializer.Deserialize<List<WeekRationMealSlotDto>>(jsonText, RationJsonOptions);
        }
        catch (JsonException)
        {
            await SendAsync(
                new WeekRationResponseDto
                {
                    Error = "Не удалось разобрать JSON ответа модели.",
                    RawAssistantContent = message.Content
                },
                statusCode: 502,
                cancellation: ct);
            return;
        }

        if (slots is null)
        {
            await SendAsync(
                new WeekRationResponseDto
                {
                    Error = "Ответ модели не удалось разобрать как список приёмов пищи.",
                    RawAssistantContent = message.Content
                },
                statusCode: 502,
                cancellation: ct);
            return;
        }

        if (!IsValidWeekRationSlots(slots, out var slotValidationError))
        {
            await SendAsync(
                new WeekRationResponseDto
                {
                    Error = slotValidationError,
                    RawAssistantContent = message.Content
                },
                statusCode: 502,
                cancellation: ct);
            return;
        }

        var productIds = WeekRationEnrichment.CollectProductIds(slots);
        var productsById = await _productsService.GetProductsByIdsAsync(productIds, ct);
        WeekRationEnrichment.AttachProducts(slots, productsById);

        await SendAsync(new WeekRationResponseDto { Ration = slots }, cancellation: ct);
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

    private static readonly string[] WeekRationMealTypes = ["breakfast", "lunch", "snack", "dinner"];

    private static bool IsValidWeekRationSlots(IReadOnlyList<WeekRationMealSlotDto> slots, out string errorMessage)
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

public sealed class WeekRationRequest
{
    /// <summary>Идентификатор сохранённого скана RPPG (результата сканирования).</summary>
    public Guid ScanId { get; set; }

    public string? Model { get; set; }

    public double? Temperature { get; set; }

    public int? MaxTokens { get; set; }

    public double? TopP { get; set; }
}

internal static class DailyEnergyEstimate
{
    /// <summary>Mifflin–St Jeor BMR × 1,375 (лёгкая активность), целые ккал.</summary>
    public static int? EstimateMaintenanceKcal(int? age, Gender? gender, int? weightKg, int? heightCm)
    {
        if (age is < 14 or > 100 || weightKg is < 35 or > 250 || heightCm is < 120 or > 230 || gender is null)
            return null;

        var w = weightKg!.Value;
        var h = heightCm!.Value;
        var a = age!.Value;
        var bmr = gender.Value == Gender.Female
            ? 10 * w + 6.25m * h - 5 * a - 161
            : 10 * w + 6.25m * h - 5 * a + 5;
        var tdee = bmr * 1.375m;
        return (int)decimal.Round(tdee, 0, MidpointRounding.AwayFromZero);
    }
}
