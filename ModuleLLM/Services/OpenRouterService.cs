using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using ModuleLLM.Configuration;
using ModuleLLM.Models.OpenRouter;
using Shared.Configs;
using Shared.Contracts;
using Shared.Extensions;

namespace ModuleLLM.Services;

public class OpenRouterService : ILlmApiService
{
    private static readonly JsonSerializerOptions LogJsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly OpenRouterApiConfiguration _config;
    private readonly ILogger<OpenRouterService> _logger;
    private readonly ILlmUsageJournal _llmUsageJournal;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    public OpenRouterService(
        IHttpClientFactory httpClientFactory,
        OpenRouterApiConfiguration config,
        ProxyConfiguration proxyConfig,
        ILogger<OpenRouterService> logger,
        ILlmUsageJournal llmUsageJournal)
    {
        _config = config;
        _logger = logger;
        _llmUsageJournal = llmUsageJournal;
        _baseUrl = _config.BaseUrl.TrimEnd('/');
        _apiKey = _config.ApiKey;

        if (proxyConfig.Enabled && !string.IsNullOrEmpty(proxyConfig.Host))
        {
            var handler = new SocketsHttpHandler
            {
                Proxy = CreateProxy(proxyConfig)
            };

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(_baseUrl),
                Timeout = TimeSpan.FromSeconds(_config.Timeout)
            };
        }
        else
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_config.Timeout);
        }
    }

    private static IWebProxy? CreateProxy(ProxyConfiguration proxyConfig)
    {
        if (!proxyConfig.Enabled || string.IsNullOrEmpty(proxyConfig.Host))
            return null;

        var proxyUri = $"{proxyConfig.Protocol.ToLowerInvariant()}://{proxyConfig.Host}:{proxyConfig.Port}";
        var webProxy = new WebProxy(proxyUri);

        if (!string.IsNullOrEmpty(proxyConfig.Username) && !string.IsNullOrEmpty(proxyConfig.Password))
            webProxy.Credentials = new NetworkCredential(proxyConfig.Username, proxyConfig.Password);

        return webProxy;
    }

    /// <inheritdoc />
    public async Task<string> ProcessAsync(
        string transcription,
        string prompt,
        bool isJsonResponse = true,
        CancellationToken cancellationToken = default)
    {
        var request = new OpenRouterChatRequest
        {
            Model = _config.Model,
            ResponseFormat = isJsonResponse
                ? new OpenRouterResponseFormat { Type = "json_object" }
                : null,
            Messages =
            [
                new OpenRouterMessage { Role = "system", Content = prompt },
                new OpenRouterMessage { Role = "user", Content = transcription }
            ]
        };

        var result = await SendChatCompletionAsync(request, cancellationToken);

        if (!result.IsSuccess || result.Value == null)
        {
            _logger.LogWarning(
                "Ошибка при вызове LLM API (OpenRouter): {Error}",
                result.Error ?? "Неизвестная ошибка");
            throw new Exception("OpenRouter LLM вернул ошибочный ответ: " + result.ToJson());
        }

        var response = result.Value;

        if (response.Choices == null || response.Choices.Count == 0)
            throw new Exception("Ответ от OpenRouter не содержит choices");

        var firstChoice = response.Choices[0];
        if (firstChoice.Message == null || string.IsNullOrWhiteSpace(firstChoice.Message.Content))
            throw new Exception("Ответ от OpenRouter не содержит содержимого");

        _logger.LogInformation("Успешно обработана транскрипция консультации через LLM (OpenRouter)");
        return firstChoice.Message.Content;
    }

    /// <inheritdoc />
    public async Task<Result<OpenRouterChatResponse>> SendChatCompletionAsync(
        OpenRouterChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt < _config.MaxRetryAttempts)
        {
            string? jsonContent = null;
            var sw = Stopwatch.StartNew();
            try
            {
                attempt++;
                _logger.LogInformation(
                    "Отправка запроса к OpenRouter API (попытка {Attempt}/{MaxAttempts})",
                    attempt,
                    _config.MaxRetryAttempts);

                var jsonOptions = new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                JsonNode? bodyNode;
                try
                {
                    bodyNode = JsonSerializer.SerializeToNode(request, jsonOptions);
                }
                catch (JsonException ex)
                {
                    sw.Stop();
                    var err = $"Не удалось сформировать тело запроса OpenRouter: {ex.Message}";
                    await JournalChatAsync(
                        SerializeRequestForLog(request),
                        sw.ElapsedMilliseconds,
                        false,
                        null,
                        err,
                        null,
                        null,
                        request.Model,
                        null,
                        null,
                        cancellationToken);
                    return Result.Failure<OpenRouterChatResponse>(err);
                }

                if (bodyNode == null)
                {
                    sw.Stop();
                    const string err = "Не удалось сформировать тело запроса OpenRouter.";
                    await JournalChatAsync(
                        SerializeRequestForLog(request),
                        sw.ElapsedMilliseconds,
                        false,
                        null,
                        err,
                        null,
                        null,
                        request.Model,
                        null,
                        null,
                        cancellationToken);
                    return Result.Failure<OpenRouterChatResponse>(err);
                }

                if (!string.IsNullOrWhiteSpace(request.ResponseFormatJson))
                {
                    try
                    {
                        bodyNode["response_format"] = JsonNode.Parse(request.ResponseFormatJson);
                    }
                    catch (JsonException ex)
                    {
                        sw.Stop();
                        var err =
                            $"Некорректный JSON в {nameof(OpenRouterChatRequest.ResponseFormatJson)}: {ex.Message}";
                        jsonContent = bodyNode.ToJsonString(jsonOptions);
                        await JournalChatAsync(
                            jsonContent,
                            sw.ElapsedMilliseconds,
                            false,
                            null,
                            err,
                            null,
                            null,
                            request.Model,
                            null,
                            null,
                            cancellationToken);
                        return Result.Failure<OpenRouterChatResponse>(err);
                    }
                }

                if (request.Provider == null && _config.Provider is not null)
                    bodyNode["provider"] = JsonSerializer.SerializeToNode(_config.Provider, jsonOptions);

                jsonContent = bodyNode.ToJsonString(jsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions")
                {
                    Content = content,
                    Headers = { { "Authorization", $"Bearer {_apiKey}" } }
                };

                var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    sw.Stop();

                    if (request.Stream)
                    {
                        var err =
                            "Streaming ответы требуют специальной обработки SSE формата.";
                        _logger.LogWarning(
                            "Streaming ответы OpenRouter требуют обработки SSE. Установите stream = false.");
                        await JournalChatAsync(
                            jsonContent,
                            sw.ElapsedMilliseconds,
                            false,
                            null,
                            err,
                            null,
                            null,
                            request.Model,
                            null,
                            null,
                            cancellationToken);
                        return Result.Failure<OpenRouterChatResponse>(err);
                    }

                    var chatResponse = JsonSerializer.Deserialize<OpenRouterChatResponse>(
                        responseContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (chatResponse == null)
                    {
                        await JournalChatAsync(
                            jsonContent,
                            sw.ElapsedMilliseconds,
                            false,
                            responseContent,
                            "Не удалось десериализовать ответ от OpenRouter",
                            null,
                            null,
                            request.Model,
                            null,
                            null,
                            cancellationToken);
                        return Result.Failure<OpenRouterChatResponse>(
                            "Не удалось десериализовать ответ от OpenRouter");
                    }

                    var assistantText = chatResponse.Choices?.FirstOrDefault()?.Message?.Content;
                    var usage = chatResponse.Usage;
                    await JournalChatAsync(
                        jsonContent,
                        sw.ElapsedMilliseconds,
                        true,
                        assistantText,
                        null,
                        usage?.PromptTokens,
                        usage?.CompletionTokens,
                        string.IsNullOrWhiteSpace(chatResponse.Model) ? request.Model : chatResponse.Model,
                        LlmUsageJournalSupport.ToDecimalCost(usage?.Cost),
                        string.IsNullOrWhiteSpace(chatResponse.Id) ? null : chatResponse.Id,
                        cancellationToken);

                    _logger.LogInformation("Успешный ответ от OpenRouter API (попытка {Attempt})", attempt);
                    return Result.Success(chatResponse);
                }

                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                sw.Stop();
                var errorResponse = JsonSerializer.Deserialize<OpenRouterErrorResponse>(
                    errorContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var errorMessage = errorResponse?.Error?.Message ?? $"HTTP {response.StatusCode}: {errorContent}";

                await JournalChatAsync(
                    jsonContent,
                    sw.ElapsedMilliseconds,
                    false,
                    null,
                    errorMessage,
                    null,
                    null,
                    request.Model,
                    null,
                    null,
                    cancellationToken);

                if (response.StatusCode >= HttpStatusCode.BadRequest &&
                    response.StatusCode < HttpStatusCode.InternalServerError)
                {
                    _logger.LogError(
                        "Ошибка клиента от OpenRouter API (не повторяем): {StatusCode} - {Error}",
                        response.StatusCode,
                        errorMessage);
                    return Result.Failure<OpenRouterChatResponse>(errorMessage);
                }

                _logger.LogWarning(
                    "Ошибка от OpenRouter API (попытка {Attempt}/{MaxAttempts}): {StatusCode} - {Error}",
                    attempt,
                    _config.MaxRetryAttempts,
                    response.StatusCode,
                    errorMessage);

                if (attempt < _config.MaxRetryAttempts)
                {
                    var delay = _config.RetryDelayMs * attempt;
                    _logger.LogInformation("Повтор через {Delay}ms", delay);
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                return Result.Failure<OpenRouterChatResponse>(errorMessage);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                sw.Stop();
                lastException = ex;
                _logger.LogError(
                    "Таймаут при запросе к OpenRouter API (попытка {Attempt}/{MaxAttempts})",
                    attempt,
                    _config.MaxRetryAttempts);

                await JournalChatAsync(
                    jsonContent ?? SerializeRequestForLog(request),
                    sw.ElapsedMilliseconds,
                    false,
                    null,
                    ex.Message,
                    null,
                    null,
                    request.Model,
                    null,
                    null,
                    cancellationToken);

                if (attempt < _config.MaxRetryAttempts)
                {
                    await Task.Delay(_config.RetryDelayMs * attempt, cancellationToken);
                    continue;
                }
            }
            catch (HttpRequestException ex)
            {
                sw.Stop();
                lastException = ex;
                _logger.LogError(
                    ex,
                    "Ошибка сети при запросе к OpenRouter API (попытка {Attempt}/{MaxAttempts})",
                    attempt,
                    _config.MaxRetryAttempts);

                await JournalChatAsync(
                    jsonContent ?? SerializeRequestForLog(request),
                    sw.ElapsedMilliseconds,
                    false,
                    null,
                    ex.Message,
                    null,
                    null,
                    request.Model,
                    null,
                    null,
                    cancellationToken);

                if (attempt < _config.MaxRetryAttempts)
                {
                    await Task.Delay(_config.RetryDelayMs * attempt, cancellationToken);
                    continue;
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                lastException = ex;
                _logger.LogError(
                    ex,
                    "Неожиданная ошибка при запросе к OpenRouter API (попытка {Attempt}/{MaxAttempts})",
                    attempt,
                    _config.MaxRetryAttempts);

                var errMsg = $"Исключение при запросе: {ex.Message}";
                await JournalChatAsync(
                    jsonContent ?? SerializeRequestForLog(request),
                    sw.ElapsedMilliseconds,
                    false,
                    null,
                    errMsg,
                    null,
                    null,
                    request.Model,
                    null,
                    null,
                    cancellationToken);

                return Result.Failure<OpenRouterChatResponse>(errMsg);
            }
        }

        return Result.Failure<OpenRouterChatResponse>(
            $"Не удалось выполнить запрос OpenRouter после {_config.MaxRetryAttempts} попыток. " +
            $"Последняя ошибка: {lastException?.Message ?? "Неизвестная ошибка"}");
    }

    private static string SerializeRequestForLog(OpenRouterChatRequest request)
    {
        try
        {
            return JsonSerializer.Serialize(request, LogJsonOptions);
        }
        catch
        {
            return "{\"error\":\"request_serialization_failed\"}";
        }
    }

    private Task JournalChatAsync(
        string inputJson,
        long durationMs,
        bool isSuccess,
        string? llmResponse,
        string? errorMessage,
        int? promptTokens,
        int? completionTokens,
        string? llmModel,
        decimal? cost,
        string? llmRequestId,
        CancellationToken cancellationToken) =>
        LlmUsageJournalSupport.TryAppendAsync(
            _llmUsageJournal,
            _logger,
            new LlmUsageJournalEntry(
                inputJson,
                durationMs,
                isSuccess,
                llmResponse,
                errorMessage,
                promptTokens,
                completionTokens,
                llmModel,
                cost,
                llmRequestId),
            cancellationToken);
}
