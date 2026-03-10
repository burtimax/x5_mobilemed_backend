using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModuleLLM.Configuration;
using ModuleLLM.Models;
using Shared.Configs;
using Shared.Contracts;
using Shared.Extensions;

namespace ModuleLLM.Services;

/// <summary>
/// Сервис для работы с RouterAI API
/// </summary>
public class RouterAIApiService : ILlmApiService
{
    private readonly HttpClient _httpClient;
    private readonly RouterAIApiConfiguration _config;
    private readonly ILogger<RouterAIApiService> _logger;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    public RouterAIApiService(
        IHttpClientFactory httpClientFactory,
        RouterAIApiConfiguration config,
        ProxyConfiguration proxyConfig,
        ILogger<RouterAIApiService> logger)
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
    public async Task<string> ProcessConsultationTranscriptionAsync(
        string transcription,
        string prompt,
        bool isJsonResponse = true,
        CancellationToken cancellationToken = default)
    {
        var request = new RouterAIChatRequest
        {
            Model = _config.Model,
            ResponseFormat = isJsonResponse
                ? new ResponseFormatType { Type = "json_object" }
                : null,
            Provider = new RouterAIProvider { Country = _config.ProviderCountry }, //ToDo вернуть ру модель
            Messages =
            [
                new GroqMessage { Role = "system", Content = prompt },
                new GroqMessage { Role = "user", Content = transcription }
            ]
        };

        var result = await SendChatCompletionAsync(request, cancellationToken);

        if (!result.IsSuccess || result.Value == null)
        {
            _logger.LogWarning(
                "Ошибка при вызове LLM API (RouterAI): {Error}",
                result.Error ?? "Неизвестная ошибка");
            throw new Exception("RouterAI LLM вернул ошибочный ответ: " + result.ToJson());
        }

        var response = result.Value;

        if (response.Choices == null || response.Choices.Count == 0)
            throw new Exception("RouterAI LLM ответ не содержит choices");

        var firstChoice = response.Choices[0];
        if (firstChoice.Message == null || string.IsNullOrWhiteSpace(firstChoice.Message.Content))
            throw new Exception("RouterAI LLM ответ не содержит содержимого");

        _logger.LogInformation("RouterAI LLM успешный запрос");
        return firstChoice.Message.Content;
    }

    /// <inheritdoc />
    public async Task<Result<GroqChatResponse>> SendChatCompletionAsync(
        RouterAIChatRequest request,
        CancellationToken cancellationToken = default)
    {
        int attempt = 0;
        Exception? lastException = null;

        while (attempt < _config.MaxRetryAttempts)
        {
            try
            {
                attempt++;
                _logger.LogInformation(
                    "Отправка запроса к RouterAI API (попытка {Attempt}/{MaxAttempts})",
                    attempt,
                    _config.MaxRetryAttempts);

                var jsonOptions = new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                var jsonContent = JsonSerializer.Serialize(request, jsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/v1/chat/completions")
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
                            "Streaming ответы требуют специальной обработки SSE формата. Установите stream = false.");
                        return Result.Failure<GroqChatResponse>(
                            "Streaming ответы требуют специальной обработки SSE формата.");
                    }

                    var chatResponse = JsonSerializer.Deserialize<GroqChatResponse>(
                        responseContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (chatResponse == null)
                        return Result.Failure<GroqChatResponse>("Не удалось десериализовать ответ от RouterAI API");

                    _logger.LogInformation("Успешный ответ от RouterAI API (попытка {Attempt})", attempt);
                    return Result.Success(chatResponse);
                }

                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var errorResponse = JsonSerializer.Deserialize<GroqErrorResponse>(
                    errorContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var errorMessage = errorResponse?.Error?.Message ?? $"HTTP {response.StatusCode}: {errorContent}";

                if (response.StatusCode >= HttpStatusCode.BadRequest &&
                    response.StatusCode < HttpStatusCode.InternalServerError)
                {
                    _logger.LogError(
                        "Ошибка клиента от RouterAI API (не повторяем): {StatusCode} - {Error}",
                        response.StatusCode,
                        errorMessage);
                    return Result.Failure<GroqChatResponse>(errorMessage);
                }

                _logger.LogWarning(
                    "Ошибка от RouterAI API (попытка {Attempt}/{MaxAttempts}): {StatusCode} - {Error}",
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

                return Result.Failure<GroqChatResponse>(errorMessage);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                lastException = ex;
                _logger.LogError(
                    "Таймаут при запросе к RouterAI API (попытка {Attempt}/{MaxAttempts})",
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
                _logger.LogError(
                    ex,
                    "Ошибка сети при запросе к RouterAI API (попытка {Attempt}/{MaxAttempts})",
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
                    "Неожиданная ошибка при запросе к RouterAI API (попытка {Attempt}/{MaxAttempts})",
                    attempt,
                    _config.MaxRetryAttempts);
                return Result.Failure<GroqChatResponse>($"Исключение при запросе: {ex.Message}");
            }
        }

        return Result.Failure<GroqChatResponse>(
            $"Не удалось выполнить запрос RouterAI после {_config.MaxRetryAttempts} попыток. " +
            $"Последняя ошибка: {lastException?.Message ?? "Неизвестная ошибка"}");
    }
}
