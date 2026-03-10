using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModuleLLM.Configuration;
using ModuleLLM.Models.GigaChat;
using Shared.Configs;
using Shared.Contracts;
using Shared.Extensions;

namespace ModuleLLM.Services;

/// <summary>
/// Сервис для работы с GigaChat API.
/// Перед каждым запросом к LLM проверяет и при необходимости обновляет access token.
/// </summary>
public class GigaChatApiService : ILlmApiService
{
    private readonly HttpClient _authHttpClient;
    private readonly HttpClient _chatHttpClient;
    private readonly GigaChatApiConfiguration _config;
    private readonly ILogger<GigaChatApiService> _logger;
    private readonly string _basicAuth;

    private string? _accessToken;
    private long _tokenExpiresAt;
    private readonly object _tokenLock = new();

    public GigaChatApiService(
        IHttpClientFactory httpClientFactory,
        GigaChatApiConfiguration config,
        ProxyConfiguration proxyConfig,
        ILogger<GigaChatApiService> logger)
    {
        _config = config;
        _logger = logger;

        var chatBaseUrl = _config.ChatBaseUrl.TrimEnd('/');

        _basicAuth = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ClientSecret}"));

        var useCustomHandler = _config.IgnoreSslCertificateErrors || (proxyConfig.Enabled && !string.IsNullOrEmpty(proxyConfig.Host));

        if (useCustomHandler)
        {
            var authHandler = CreateHttpHandler(proxyConfig);
            var chatHandler = CreateHttpHandler(proxyConfig);
            _authHttpClient = new HttpClient(authHandler, disposeHandler: true)
            {
                Timeout = TimeSpan.FromSeconds(_config.Timeout)
            };
            _chatHttpClient = new HttpClient(chatHandler, disposeHandler: true)
            {
                BaseAddress = new Uri(chatBaseUrl),
                Timeout = TimeSpan.FromSeconds(_config.Timeout)
            };
        }
        else
        {
            _authHttpClient = httpClientFactory.CreateClient();
            _authHttpClient.Timeout = TimeSpan.FromSeconds(_config.Timeout);

            _chatHttpClient = httpClientFactory.CreateClient();
            _chatHttpClient.BaseAddress = new Uri(chatBaseUrl);
            _chatHttpClient.Timeout = TimeSpan.FromSeconds(_config.Timeout);
        }
    }

    private HttpMessageHandler CreateHttpHandler(ProxyConfiguration proxyConfig)
    {
        if (_config.IgnoreSslCertificateErrors)
        {
            var handler = new HttpClientHandler
            {
                Proxy = CreateProxy(proxyConfig),
                ServerCertificateCustomValidationCallback = static (_, _, _, _) => true
            };
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            return handler;
        }

        var socketsHandler = new SocketsHttpHandler
        {
            Proxy = CreateProxy(proxyConfig),
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        };
        return socketsHandler;
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
        var request = new GigaChatChatRequest
        {
            Model = _config.Model,
            MaxTokens = _config.MaxTokens,
            ResponseFormat = isJsonResponse ? new GigaChatResponseFormat { Type = "json_object" } : null,
            Messages =
            [
                new GigaChatMessage { Role = "system", Content = prompt },
                new GigaChatMessage { Role = "user", Content = transcription }
            ]
        };

        var result = await SendChatCompletionAsync(request, cancellationToken);

        if (!result.IsSuccess || result.Value == null)
        {
            _logger.LogWarning("Ошибка при вызове LLM API (GigaChat): {Error}", result.Error ?? "Неизвестная ошибка");
            throw new Exception("GigaChat LLM вернул ошибочный ответ: " + result.ToJson());
        }

        var response = result.Value;

        if (response.Choices == null || response.Choices.Count == 0)
            throw new Exception("Ответ от LLM не содержит choices");

        var firstChoice = response.Choices[0];
        if (firstChoice.Message == null || string.IsNullOrWhiteSpace(firstChoice.Message.Content))
            throw new Exception("GigaChat LLM ответ не содержит содержимого");

        _logger.LogInformation("GigaChat LLM успешный запрос");
        return firstChoice.Message.Content;
    }

    /// <summary>
    /// Отправляет запрос к GigaChat chat completions API
    /// </summary>
    public async Task<Result<GigaChatChatResponse>> SendChatCompletionAsync(
        GigaChatChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var tokenResult = await GetAccessTokenAsync(cancellationToken);
        if (!tokenResult.IsSuccess || string.IsNullOrEmpty(tokenResult.Value))
            return Result.Failure<GigaChatChatResponse>(tokenResult.Error ?? "Не удалось получить access token");

        var accessToken = tokenResult.Value!;

        int attempt = 0;
        Exception? lastException = null;

        while (attempt < _config.MaxRetryAttempts)
        {
            try
            {
                attempt++;
                _logger.LogInformation(
                    "Отправка запроса к GigaChat API (попытка {Attempt}/{MaxAttempts})",
                    attempt,
                    _config.MaxRetryAttempts);

                var jsonOptions = new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                var jsonContent = JsonSerializer.Serialize(request, jsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(_chatHttpClient.BaseAddress!, "/api/v1/chat/completions"))
                {
                    Content = content,
                    Headers =
                    {
                        { "Authorization", $"Bearer {accessToken}" },
                        { "Accept", "application/json" }
                    }
                };

                var response = await _chatHttpClient.SendAsync(httpRequest, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                    if (request.Stream)
                    {
                        _logger.LogWarning("Streaming ответы требуют специальной обработки SSE формата.");
                        return Result.Failure<GigaChatChatResponse>(
                            "Streaming ответы требуют специальной обработки SSE формата.");
                    }

                    var chatResponse = JsonSerializer.Deserialize<GigaChatChatResponse>(
                        responseContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (chatResponse == null)
                        return Result.Failure<GigaChatChatResponse>("Не удалось десериализовать ответ от GigaChat API");

                    _logger.LogInformation("Успешный ответ от GigaChat API (попытка {Attempt})", attempt);
                    return Result.Success(chatResponse);
                }

                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var errorMessage = ParseErrorMessage(errorContent, response.StatusCode);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Access token истёк или недействителен, обновляю токен");
                    lock (_tokenLock)
                    {
                        _accessToken = null;
                        _tokenExpiresAt = 0;
                    }

                    tokenResult = await GetAccessTokenAsync(cancellationToken);
                    if (tokenResult.IsSuccess)
                    {
                        accessToken = tokenResult.Value!;
                        continue;
                    }
                }

                if (response.StatusCode >= HttpStatusCode.BadRequest &&
                    response.StatusCode < HttpStatusCode.InternalServerError)
                {
                    _logger.LogError(
                        "Ошибка клиента от GigaChat API (не повторяем): {StatusCode} - {Error}",
                        response.StatusCode,
                        errorMessage);
                    return Result.Failure<GigaChatChatResponse>(errorMessage);
                }

                _logger.LogWarning(
                    "Ошибка от GigaChat API (попытка {Attempt}/{MaxAttempts}): {StatusCode} - {Error}",
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

                return Result.Failure<GigaChatChatResponse>(errorMessage);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                lastException = ex;
                _logger.LogWarning(
                    "Таймаут при запросе к GigaChat API (попытка {Attempt}/{MaxAttempts})",
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
                    "Ошибка сети при запросе к GigaChat API (попытка {Attempt}/{MaxAttempts})",
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
                    "Неожиданная ошибка при запросе к GigaChat API (попытка {Attempt}/{MaxAttempts})",
                    attempt,
                    _config.MaxRetryAttempts);
                return Result.Failure<GigaChatChatResponse>($"Исключение при запросе: {ex.Message}");
            }
        }

        return Result.Failure<GigaChatChatResponse>(
            $"Не удалось выполнить запрос после {_config.MaxRetryAttempts} попыток. " +
            $"Последняя ошибка: {lastException?.Message ?? "Неизвестная ошибка"}");
    }

    /// <summary>
    /// Получает access token. Кэширует токен и обновляет за TokenExpiryBufferMs до истечения.
    /// </summary>
    private async Task<Result<string>> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var buffer = _config.TokenExpiryBufferMs;

        lock (_tokenLock)
        {
            if (!string.IsNullOrEmpty(_accessToken) && _tokenExpiresAt > now + buffer)
                return Result.Success(_accessToken);
        }

        try
        {
            var rqUid = Guid.NewGuid().ToString("D");

            var authRequest = new HttpRequestMessage(HttpMethod.Post, _config.AuthUrl)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["scope"] = _config.Scope
                }),
                Headers =
                {
                    { "Authorization", $"Basic {_basicAuth}" },
                    { "RqUID", rqUid },
                    { "Accept", "application/json" }
                }
            };

            var response = await _authHttpClient.SendAsync(authRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var errorMsg = ParseTokenErrorMessage(errorContent, response.StatusCode);
                _logger.LogError("Ошибка получения токена GigaChat: {StatusCode} - {Error}",
                    response.StatusCode, errorMsg);
                return Result.Failure<string>(errorMsg);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResponse = JsonSerializer.Deserialize<GigaChatTokenResponse>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                _logger.LogError("Пустой ответ при получении токена GigaChat");
                return Result.Failure<string>("Пустой ответ при получении токена");
            }

            lock (_tokenLock)
            {
                _accessToken = tokenResponse.AccessToken;
                _tokenExpiresAt = tokenResponse.ExpiresAt;
            }

            _logger.LogInformation(
                "Успешно получен access token GigaChat, истекает в {ExpiresAt}",
                DateTimeOffset.FromUnixTimeMilliseconds(tokenResponse.ExpiresAt).UtcDateTime);

            return Result.Success(tokenResponse.AccessToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Исключение при получении токена GigaChat");
            return Result.Failure<string>($"Не удалось получить токен: {ex.Message}");
        }
    }

    private static string ParseErrorMessage(string errorContent, HttpStatusCode statusCode)
    {
        try
        {
            var err = JsonSerializer.Deserialize<GigaChatErrorResponse>(
                errorContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return err?.Error ?? err?.ErrorDescription ?? err?.Message ?? $"HTTP {(int)statusCode}: {errorContent}";
        }
        catch
        {
            return $"HTTP {(int)statusCode}: {errorContent}";
        }
    }

    private static string ParseTokenErrorMessage(string errorContent, HttpStatusCode statusCode)
    {
        try
        {
            var err = JsonSerializer.Deserialize<GigaChatErrorResponse>(
                errorContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return err?.Error ?? err?.ErrorDescription ?? err?.Message ?? $"HTTP {(int)statusCode}: {errorContent}";
        }
        catch
        {
            return $"HTTP {(int)statusCode}: {errorContent}";
        }
    }
}
