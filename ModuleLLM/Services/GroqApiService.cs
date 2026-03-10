using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModuleLLM.Configuration;
using ModuleLLM.Models;
using ModuleLLM.Prompt;
using Shared.Configs;
using Shared.Contracts;
using Shared.Extensions;

namespace ModuleLLM.Services;

/// <summary>
/// Сервис для работы с Groq API
/// </summary>
public class GroqApiService : ILlmApiService
{
    private readonly HttpClient _httpClient;
    private readonly GroqApiConfiguration _config;
    private readonly ILogger<GroqApiService> _logger;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    /// <summary>
    /// Инициализирует новый экземпляр сервиса Groq API
    /// </summary>
    public GroqApiService(
        IHttpClientFactory httpClientFactory,
        GroqApiConfiguration config,
        ProxyConfiguration proxyConfig,
        ILogger<GroqApiService> logger)
    {
        _config = config;
        _logger = logger;
        _baseUrl = _config.BaseUrl.TrimEnd('/');
        _apiKey = _config.ApiKey;

        // Настройка HttpClient с поддержкой прокси
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

            // _logger.LogInformation(
            //     "HttpClient настроен с прокси: {Protocol}://{Host}:{Port}",
            //     proxyConfig.Protocol,
            //     proxyConfig.Host,
            //     proxyConfig.Port);
        }
        else
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_config.Timeout);
        }
    }

    /// <summary>
    /// Создает прокси для HttpClient
    /// </summary>
    private static IWebProxy? CreateProxy(ProxyConfiguration proxyConfig)
    {
        if (!proxyConfig.Enabled || string.IsNullOrEmpty(proxyConfig.Host))
        {
            return null;
        }

        // Формируем URI прокси в формате protocol://host:port
        var proxyUri = $"{proxyConfig.Protocol.ToLowerInvariant()}://{proxyConfig.Host}:{proxyConfig.Port}";
        var webProxy = new WebProxy(proxyUri);

        // Если указаны учетные данные, добавляем их
        if (!string.IsNullOrEmpty(proxyConfig.Username) && !string.IsNullOrEmpty(proxyConfig.Password))
        {
            webProxy.Credentials = new NetworkCredential(proxyConfig.Username, proxyConfig.Password);
        }

        return webProxy;
    }

    /// <summary>
    /// Отправляет запрос на обработку транскрибации консультации
    /// </summary>
    public async Task<string> ProcessConsultationTranscriptionAsync(
        string transcription,
        string prompt,
        bool isJsonResponse = true,
        CancellationToken cancellationToken = default)
    {
        var request = new GroqChatRequest
        {
            ResponseFormat = isJsonResponse
                ? new(){ Type = "json_object" }
                : null,
            Messages = new List<GroqMessage>
            {
                new() { Role = "system", Content = prompt },
                new() { Role = "user", Content = transcription }
            }
        };

        var result = await SendChatCompletionAsync(request, cancellationToken);

        if (!result.IsSuccess || result.Value == null)
        {
            _logger.LogWarning(
                "Ошибка при вызове LLM API (Groq): {Error}",
                result.Error ?? "Неизвестная ошибка");
            throw new Exception("Groq LLM вернул ошибочный ответ: " + result.ToJson());
        }

        var response = result.Value;

        if (response.Choices == null || response.Choices.Count == 0)
            throw new Exception("Ответ от LLM не содержит choices");

        var firstChoice = response.Choices[0];
        if (firstChoice.Message == null || string.IsNullOrWhiteSpace(firstChoice.Message.Content))
            throw new Exception("Ответ от LLM не содержит содержимого");

        _logger.LogInformation("Успешно обработана транскрипция консультации через LLM (Groq)");
        return firstChoice.Message.Content;
    }

    /// <summary>
    /// Отправляет кастомный запрос к Groq API с retry логикой
    /// </summary>
    public async Task<Result<GroqChatResponse>> SendChatCompletionAsync(
        GroqChatRequest request,
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
                    "Отправка запроса к Groq API (попытка {Attempt}/{MaxAttempts})",
                    attempt,
                    _config.MaxRetryAttempts);

                var jsonOptions = new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                var jsonContent = JsonSerializer.Serialize(request, jsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions")
                {
                    Content = content,
                    Headers =
                    {
                        { "Authorization", $"Bearer {_apiKey}" }
                    }
                };

                var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                    // Обработка streaming ответа (если stream = true)
                    if (request.Stream)
                    {
                        _logger.LogWarning(
                            "Получен streaming ответ. Streaming ответы требуют специальной обработки SSE формата. " +
                            "Рекомендуется установить stream = false для получения полного ответа в одном запросе.");

                        // Для streaming нужно парсить SSE формат
                        // Пока возвращаем ошибку, так как streaming требует специальной обработки
                        return Result.Failure<GroqChatResponse>(
                            "Streaming ответы требуют специальной обработки SSE формата. " +
                            "Установите stream = false для получения полного ответа или реализуйте обработку SSE.");
                    }

                    var chatResponse = JsonSerializer.Deserialize<GroqChatResponse>(
                        responseContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (chatResponse == null)
                    {
                        return Result.Failure<GroqChatResponse>("Не удалось десериализовать ответ от Groq API");
                    }

                    _logger.LogInformation(
                        "Успешный ответ от Groq API (попытка {Attempt})",
                        attempt);

                    return Result.Success(chatResponse);
                }

                // Обработка ошибок от API
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var errorResponse = JsonSerializer.Deserialize<GroqErrorResponse>(
                    errorContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var errorMessage = errorResponse?.Error?.Message ?? $"HTTP {response.StatusCode}: {errorContent}";

                // Если это ошибка клиента (4xx), не повторяем запрос
                if (response.StatusCode >= HttpStatusCode.BadRequest &&
                    response.StatusCode < HttpStatusCode.InternalServerError)
                {
                    _logger.LogError(
                        "Ошибка клиента от Groq API (не повторяем): {StatusCode} - {Error}",
                        response.StatusCode,
                        errorMessage);
                    return Result.Failure<GroqChatResponse>(errorMessage);
                }

                // Для серверных ошибок (5xx) или сетевых проблем - повторяем
                _logger.LogWarning(
                    "Ошибка от Groq API (попытка {Attempt}/{MaxAttempts}): {StatusCode} - {Error}",
                    attempt,
                    _config.MaxRetryAttempts,
                    response.StatusCode,
                    errorMessage);

                if (attempt < _config.MaxRetryAttempts)
                {
                    var delay = _config.RetryDelayMs * attempt; // Exponential backoff
                    _logger.LogInformation("Повтор через {Delay}ms", delay);
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                return Result.Failure<GroqChatResponse>(errorMessage);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                lastException = ex;
                _logger.LogWarning(
                    "Таймаут при запросе к Groq API (попытка {Attempt}/{MaxAttempts})",
                    attempt,
                    _config.MaxRetryAttempts);

                if (attempt < _config.MaxRetryAttempts)
                {
                    var delay = _config.RetryDelayMs * attempt;
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                _logger.LogWarning(
                    ex,
                    "Ошибка сети при запросе к Groq API (попытка {Attempt}/{MaxAttempts})",
                    attempt,
                    _config.MaxRetryAttempts);

                if (attempt < _config.MaxRetryAttempts)
                {
                    var delay = _config.RetryDelayMs * attempt;
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogError(
                    ex,
                    "Неожиданная ошибка при запросе к Groq API (попытка {Attempt}/{MaxAttempts})",
                    attempt,
                    _config.MaxRetryAttempts);

                // Для неожиданных ошибок не повторяем
                return Result.Failure<GroqChatResponse>($"Исключение при запросе: {ex.Message}");
            }
        }

        return Result.Failure<GroqChatResponse>(
            $"Не удалось выполнить запрос Groq после {_config.MaxRetryAttempts} попыток. " +
            $"Последняя ошибка: {lastException?.Message ?? "Неизвестная ошибка"}");
    }
}

