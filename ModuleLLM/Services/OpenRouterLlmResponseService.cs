using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using ModuleLLM.Configuration;
using ModuleLLM.Models.OpenRouter;
using ModuleLLM.Models.OpenRouter.LlmResponses;
using Shared.Configs;
using Shared.Contracts;
using Shared.Extensions;

namespace ModuleLLM.Services;

public sealed class OpenRouterLlmResponseService : IOpenRouterLlmResponseService
{
    private readonly HttpClient _httpClient;
    private readonly OpenRouterApiConfiguration _config;
    private readonly ILogger<OpenRouterLlmResponseService> _logger;
    private readonly ILlmUsageJournal _llmUsageJournal;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    private static readonly JsonSerializerOptions SerializeOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OpenRouterLlmResponseService(
        IHttpClientFactory httpClientFactory,
        OpenRouterApiConfiguration config,
        ProxyConfiguration proxyConfig,
        ILogger<OpenRouterLlmResponseService> logger,
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
    public async Task<Result<OpenRouterLlmResponsesResponse>> SendAsync(
        OpenRouterLlmResponsesRequest request,
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
                    "Отправка запроса к OpenRouter Responses API (попытка {Attempt}/{MaxAttempts})",
                    attempt,
                    _config.MaxRetryAttempts);

                try
                {
                    jsonContent = JsonSerializer.Serialize(request, SerializeOptions);
                }
                catch (JsonException ex)
                {
                    sw.Stop();
                    var err = $"Не удалось сериализовать запрос OpenRouter Responses: {ex.Message}";
                    await JournalResponsesAsync(
                        SerializeResponsesRequestForLog(request),
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
                    return Result.Failure<OpenRouterLlmResponsesResponse>(err);
                }

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/responses")
                {
                    Content = content,
                    Headers = { { "Authorization", $"Bearer {_apiKey}" } }
                };

                var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    sw.Stop();

                    var body = JsonSerializer.Deserialize<OpenRouterLlmResponsesResponse>(
                        responseContent,
                        DeserializeOptions);

                    if (body == null)
                    {
                        await JournalResponsesAsync(
                            jsonContent,
                            sw.ElapsedMilliseconds,
                            false,
                            responseContent,
                            "Не удалось десериализовать ответ OpenRouter Responses",
                            null,
                            null,
                            request.Model,
                            null,
                            null,
                            cancellationToken);
                        return Result.Failure<OpenRouterLlmResponsesResponse>(
                            "Не удалось десериализовать ответ OpenRouter Responses");
                    }

                    var usage = body.Usage;
                    var errFromBody = body.Error is { Message: { } m } && !string.IsNullOrEmpty(m) ? m : null;
                    if (errFromBody != null)
                    {
                        _logger.LogError(
                            "OpenRouter Responses вернул ошибку в теле: {Message}",
                            errFromBody);
                        await JournalResponsesAsync(
                            jsonContent,
                            sw.ElapsedMilliseconds,
                            false,
                            body.GetFirstAssistantOutputText(),
                            errFromBody,
                            usage?.InputTokens,
                            usage?.OutputTokens,
                            string.IsNullOrWhiteSpace(body.Model) ? request.Model : body.Model,
                            LlmUsageJournalSupport.ToDecimalCost(usage?.Cost),
                            string.IsNullOrWhiteSpace(body.Id) ? null : body.Id,
                            cancellationToken);
                        return Result.Failure<OpenRouterLlmResponsesResponse>(errFromBody);
                    }

                    if (!string.Equals(body.Status, "completed", StringComparison.OrdinalIgnoreCase))
                    {
                        var msg = $"OpenRouter Responses: статус ответа '{body.Status}'";
                        _logger.LogWarning(msg);
                        await JournalResponsesAsync(
                            jsonContent,
                            sw.ElapsedMilliseconds,
                            false,
                            body.GetFirstAssistantOutputText(),
                            msg,
                            usage?.InputTokens,
                            usage?.OutputTokens,
                            string.IsNullOrWhiteSpace(body.Model) ? request.Model : body.Model,
                            LlmUsageJournalSupport.ToDecimalCost(usage?.Cost),
                            string.IsNullOrWhiteSpace(body.Id) ? null : body.Id,
                            cancellationToken);
                        return Result.Failure<OpenRouterLlmResponsesResponse>(msg);
                    }

                    var assistantText = body.GetFirstAssistantOutputText();
                    await JournalResponsesAsync(
                        jsonContent,
                        sw.ElapsedMilliseconds,
                        true,
                        assistantText,
                        null,
                        usage?.InputTokens,
                        usage?.OutputTokens,
                        string.IsNullOrWhiteSpace(body.Model) ? request.Model : body.Model,
                        LlmUsageJournalSupport.ToDecimalCost(usage?.Cost),
                        string.IsNullOrWhiteSpace(body.Id) ? null : body.Id,
                        cancellationToken);

                    _logger.LogInformation("Успешный ответ от OpenRouter Responses API (попытка {Attempt})", attempt);
                    return Result.Success(body);
                }

                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                sw.Stop();
                var errorResponse = JsonSerializer.Deserialize<OpenRouterErrorResponse>(
                    errorContent,
                    DeserializeOptions);

                var errorMessage = errorResponse?.Error?.Message ?? $"HTTP {response.StatusCode}: {errorContent}";

                await JournalResponsesAsync(
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
                        "Ошибка клиента от OpenRouter Responses API (не повторяем): {StatusCode} - {Error}",
                        response.StatusCode,
                        errorMessage);
                    return Result.Failure<OpenRouterLlmResponsesResponse>(errorMessage);
                }

                _logger.LogWarning(
                    "Ошибка от OpenRouter Responses API (попытка {Attempt}/{MaxAttempts}): {StatusCode} - {Error}",
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

                return Result.Failure<OpenRouterLlmResponsesResponse>(errorMessage);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                sw.Stop();
                lastException = ex;
                _logger.LogWarning(
                    "Таймаут при запросе к OpenRouter Responses API (попытка {Attempt}/{MaxAttempts})",
                    attempt,
                    _config.MaxRetryAttempts);

                await JournalResponsesAsync(
                    jsonContent ?? SerializeResponsesRequestForLog(request),
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
                _logger.LogWarning(
                    ex,
                    "Ошибка сети при запросе к OpenRouter Responses API (попытка {Attempt}/{MaxAttempts})",
                    attempt,
                    _config.MaxRetryAttempts);

                await JournalResponsesAsync(
                    jsonContent ?? SerializeResponsesRequestForLog(request),
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
                    "Неожиданная ошибка при запросе к OpenRouter Responses API (попытка {Attempt}/{MaxAttempts})",
                    attempt,
                    _config.MaxRetryAttempts);

                var errMsg = $"Исключение при запросе: {ex.Message}";
                await JournalResponsesAsync(
                    jsonContent ?? SerializeResponsesRequestForLog(request),
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

                return Result.Failure<OpenRouterLlmResponsesResponse>(errMsg);
            }
        }

        return Result.Failure<OpenRouterLlmResponsesResponse>(
            $"Не удалось выполнить запрос OpenRouter Responses после {_config.MaxRetryAttempts} попыток. " +
            $"Последняя ошибка: {lastException?.Message ?? "Неизвестная ошибка"}");
    }

    private static string SerializeResponsesRequestForLog(OpenRouterLlmResponsesRequest request)
    {
        try
        {
            return JsonSerializer.Serialize(request, SerializeOptions);
        }
        catch
        {
            return "{\"error\":\"request_serialization_failed\"}";
        }
    }

    private Task JournalResponsesAsync(
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
