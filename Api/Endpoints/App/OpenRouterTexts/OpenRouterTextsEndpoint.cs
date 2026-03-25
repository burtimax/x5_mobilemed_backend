using System.ComponentModel;
using Api.Endpoints.App;
using FastEndpoints;
using ModuleLLM.Configuration;
using ModuleLLM.Models.OpenRouter.LlmResponses;
using ModuleLLM.Services;

namespace Api.Endpoints.App.OpenRouterTexts;

/// <summary>
/// Принимает список текстов, отправляет их в OpenRouter Responses API и возвращает сгенерированный текст.
/// </summary>
public sealed class OpenRouterTextsEndpoint : Endpoint<OpenRouterTextsRequest, OpenRouterTextsResponse>
{
    private const string DefaultSystemPrompt =
        "Ниже передан набор текстовых фрагментов. Дай один связный ответ на русском языке, опираясь на все фрагменты.";

    private readonly IOpenRouterLlmResponseService _responsesService;
    private readonly OpenRouterApiConfiguration _openRouterConfig;

    public OpenRouterTextsEndpoint(
        IOpenRouterLlmResponseService responsesService,
        OpenRouterApiConfiguration openRouterConfig)
    {
        _responsesService = responsesService;
        _openRouterConfig = openRouterConfig;
    }

    public override void Configure()
    {
        Post("openrouter/texts");
        AllowAnonymous();
        Group<AppGroupEndpoints>();
        Summary(s =>
        {
            s.Summary = "OpenRouter: обработка списка текстов";
            s.Description =
                "Тело запроса содержит массив строк; они передаются модели как отдельные фрагменты. В ответе — один текст от модели.";
        });
    }

    public override async Task HandleAsync(OpenRouterTextsRequest req, CancellationToken ct)
    {
        if (req.Texts is null || req.Texts.Count == 0)
        {
            await SendAsync(
                new OpenRouterTextsResponse { Text = string.Empty, Error = "Укажите непустой список texts." },
                statusCode: 400,
                cancellation: ct);
            return;
        }

        var parts = new List<OpenRouterLlmInputContentPart>();
        foreach (var raw in req.Texts)
        {
            var t = raw?.Trim();
            if (string.IsNullOrEmpty(t))
                continue;
            parts.Add(new OpenRouterLlmInputTextPart { Text = t });
        }

        if (parts.Count == 0)
        {
            await SendAsync(
                new OpenRouterTextsResponse { Text = string.Empty, Error = "Все элементы texts пустые." },
                statusCode: 400,
                cancellation: ct);
            return;
        }

        var model = string.IsNullOrWhiteSpace(req.Model) ? _openRouterConfig.Model : req.Model.Trim();
        var systemPrompt = string.IsNullOrWhiteSpace(req.SystemPrompt)
            ? DefaultSystemPrompt
            : req.SystemPrompt.Trim();

        var llmRequest = new OpenRouterLlmResponsesRequest
        {
            Model = model,
            Input =
            [
                new OpenRouterLlmResponsesInputItem
                {
                    Role = "system",
                    Content = OpenRouterLlmResponsesInputContent.FromString(systemPrompt)
                },
                new OpenRouterLlmResponsesInputItem
                {
                    Role = "user",
                    Content = OpenRouterLlmResponsesInputContent.FromParts(parts)
                }
            ]
        };

        var result = await _responsesService.SendAsync(llmRequest, ct);

        if (!result.IsSuccess || result.Value is null)
        {
            await SendAsync(
                new OpenRouterTextsResponse { Text = string.Empty, Error = result.Error ?? "Ошибка OpenRouter." },
                statusCode: 502,
                cancellation: ct);
            return;
        }

        var reply = result.Value.GetFirstAssistantOutputText();
        if (string.IsNullOrWhiteSpace(reply))
        {
            await SendAsync(
                new OpenRouterTextsResponse
                {
                    Text = string.Empty,
                    Error = "Модель вернула пустой текст."
                },
                statusCode: 502,
                cancellation: ct);
            return;
        }

        await SendAsync(new OpenRouterTextsResponse { Text = reply }, cancellation: ct);
    }
}

public sealed class OpenRouterTextsRequest
{
    /// <summary>Текстовые фрагменты (пустые строки пропускаются).</summary>
    public List<string> Texts { get; set; } = new();

    /// <summary>Необязательная инструкция для роли system (иначе — встроенный промпт по умолчанию).</summary>
    [DefaultValue("Ты ИИ помощник")]
    public string? SystemPrompt { get; set; }

    /// <summary>Необязательная модель OpenRouter (иначе из конфигурации).</summary>
    [DefaultValue("google/gemini-2.5-flash")]
    public string? Model { get; set; }
}

public sealed class OpenRouterTextsResponse
{
    public string Text { get; set; } = string.Empty;

    /// <summary>Сообщение об ошибке при HTTP 4xx/502.</summary>
    public string? Error { get; set; }
}
