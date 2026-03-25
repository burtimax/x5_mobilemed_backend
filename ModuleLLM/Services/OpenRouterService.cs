using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using ModuleLLM.Configuration;
using ModuleLLM.Models.OpenRouter;
using Shared.Configs;
using Shared.Contracts;
using Shared.Extensions;

namespace ModuleLLM.Services;

public class OpenRouterService : ILlmApiService
{
    private readonly HttpClient _httpClient;
    private readonly OpenRouterApiConfiguration _config;
    private readonly ILogger<OpenRouterService> _logger;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    public OpenRouterService(
        IHttpClientFactory httpClientFactory,
        OpenRouterApiConfiguration config,
        ProxyConfiguration proxyConfig,
        ILogger<OpenRouterService> logger)
    {
        _config = config;
        _logger = logger;
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
            try
            {
                attempt++;
                _logger.LogInformation(
                    "Отправка запроса к OpenRouter API (попытка {Attempt}/{MaxAttempts})",
                    attempt,
                    _config.MaxRetryAttempts);

                var jsonOptions = new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                JsonNode? bodyNode;
                try
                {
                    bodyNode = JsonSerializer.SerializeToNode(request, jsonOptions);
                }
                catch (JsonException ex)
                {
                    return Result.Failure<OpenRouterChatResponse>(
                        $"Не удалось сформировать тело запроса OpenRouter: {ex.Message}");
                }

                if (bodyNode == null)
                    return Result.Failure<OpenRouterChatResponse>("Не удалось сформировать тело запроса OpenRouter.");

                if (!string.IsNullOrWhiteSpace(request.ResponseFormatJson))
                {
                    try
                    {
                        bodyNode["response_format"] = JsonNode.Parse(request.ResponseFormatJson);
                    }
                    catch (JsonException ex)
                    {
                        return Result.Failure<OpenRouterChatResponse>(
                            $"Некорректный JSON в {nameof(OpenRouterChatRequest.ResponseFormatJson)}: {ex.Message}");
                    }
                }

                var jsonContent = bodyNode.ToJsonString(jsonOptions);
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

                    if (request.Stream)
                    {
                        _logger.LogWarning(
                            "Streaming ответы OpenRouter требуют обработки SSE. Установите stream = false.");
                        return Result.Failure<OpenRouterChatResponse>(
                            "Streaming ответы требуют специальной обработки SSE формата.");
                    }

                    var chatResponse = JsonSerializer.Deserialize<OpenRouterChatResponse>(
                        responseContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (chatResponse == null)
                        return Result.Failure<OpenRouterChatResponse>("Не удалось десериализовать ответ от OpenRouter");

                    _logger.LogInformation("Успешный ответ от OpenRouter API (попытка {Attempt})", attempt);
                    return Result.Success(chatResponse);
                }

                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var errorResponse = JsonSerializer.Deserialize<OpenRouterErrorResponse>(
                    errorContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var errorMessage = errorResponse?.Error?.Message ?? $"HTTP {response.StatusCode}: {errorContent}";

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
                lastException = ex;
                _logger.LogWarning(
                    "Таймаут при запросе к OpenRouter API (попытка {Attempt}/{MaxAttempts})",
                    attempt,
                    _config.MaxRetryAttempts);

                if (attempt < _config.MaxRetryAttempts)
                {
                    await Task.Delay(_config.RetryDelayMs * attempt, cancellationToken);
                    continue;
                }
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                _logger.LogWarning(
                    ex,
                    "Ошибка сети при запросе к OpenRouter API (попытка {Attempt}/{MaxAttempts})",
                    attempt,
                    _config.MaxRetryAttempts);

                if (attempt < _config.MaxRetryAttempts)
                {
                    await Task.Delay(_config.RetryDelayMs * attempt, cancellationToken);
                    continue;
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogError(
                    ex,
                    "Неожиданная ошибка при запросе к OpenRouter API (попытка {Attempt}/{MaxAttempts})",
                    attempt,
                    _config.MaxRetryAttempts);
                return Result.Failure<OpenRouterChatResponse>($"Исключение при запросе: {ex.Message}");
            }
        }

        return Result.Failure<OpenRouterChatResponse>(
            $"Не удалось выполнить запрос OpenRouter после {_config.MaxRetryAttempts} попыток. " +
            $"Последняя ошибка: {lastException?.Message ?? "Неизвестная ошибка"}");
    }
}
