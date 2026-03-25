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
        + "В JSON указывай для каждого приёма пищи только идентификаторы товаров (id) из карточек каталога (поле ID: в тексте). "
        + "Опирайся на отчёт сканирования и калорийность из профиля. "
        + "Не включай товары из списка исключений пользователя. "
        + "Ответ — корневой JSON-массив из 7 объектов: day 1–7 без повторов, у каждого дня массивы breakfast, lunch, snack, dinner; в массивах только объекты с полем id (целое число из каталога).";

    private static readonly string WeekRationResponseFormatJson =
        """
        {
          "type": "json_schema",
          "json_schema": {
            "name": "week_ration",
            "strict": true,
            "schema": {
              "type": "array",
              "description": "Список приёмов пищи по дням недели. Каждый объект содержит номер дня и один тип приёма пищи с массивом товаров.",
              "items": {
                "type": "object",
                "additionalProperties": false,
                "properties": {
                  "day": {
                    "type": "integer",
                    "description": "Номер дня недели от 1 до 7."
                  },
                  "breakfast": {
                    "type": "array",
                    "description": "Список товаров на завтрак.",
                    "items": {
                      "type": "object",
                      "additionalProperties": false,
                      "properties": {
                        "id": {
                          "type": "integer",
                          "description": "Идентификатор товара."
                        }
                      },
                      "required": ["id"]
                    }
                  },
                  "lunch": {
                    "type": "array",
                    "description": "Список товаров на обед.",
                    "items": {
                      "type": "object",
                      "additionalProperties": false,
                      "properties": {
                        "id": {
                          "type": "integer",
                          "description": "Идентификатор товара."
                        }
                      },
                      "required": ["id"]
                    }
                  },
                  "snack": {
                    "type": "array",
                    "description": "Список товаров на перекус.",
                    "items": {
                      "type": "object",
                      "additionalProperties": false,
                      "properties": {
                        "id": {
                          "type": "integer",
                          "description": "Идентификатор товара."
                        }
                      },
                      "required": ["id"]
                    }
                  },
                  "dinner": {
                    "type": "array",
                    "description": "Список товаров на ужин.",
                    "items": {
                      "type": "object",
                      "additionalProperties": false,
                      "properties": {
                        "id": {
                          "type": "integer",
                          "description": "Идентификатор товара."
                        }
                      },
                      "required": ["id"]
                    }
                  }
                },
                "required": ["day"]
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
            + "\n\nСоставь недельный рацион: корневой JSON-массив из 7 дней в формате схемы (только id товаров).";

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

        List<WeekRationDayDto>? days;
        try
        {
            var jsonText = UnwrapAssistantJsonPayload(message.Content);
            days = JsonSerializer.Deserialize<List<WeekRationDayDto>>(jsonText, RationJsonOptions);
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

        if (days is null || days.Count != 7)
        {
            await SendAsync(
                new WeekRationResponseDto
                {
                    Error = "Ответ модели не является списком из 7 дней рациона.",
                    RawAssistantContent = message.Content
                },
                statusCode: 502,
                cancellation: ct);
            return;
        }

        var productIds = WeekRationEnrichment.CollectProductIds(days);
        var productsById = await _productsService.GetProductsByIdsAsync(productIds, ct);
        WeekRationEnrichment.AttachProducts(days, productsById);

        await SendAsync(new WeekRationResponseDto { Ration = days }, cancellation: ct);
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
