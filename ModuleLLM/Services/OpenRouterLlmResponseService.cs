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
        ILogger<OpenRouterLlmResponseService> logger)
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
    public async Task<Result<OpenRouterLlmResponsesResponse>> SendAsync(
        OpenRouterLlmResponsesRequest request,
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
                    "Отправка запроса к OpenRouter Responses API (попытка {Attempt}/{MaxAttempts})",
                    attempt,
                    _config.MaxRetryAttempts);

                var jsonContent = JsonSerializer.Serialize(request, SerializeOptions);
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
                    var body = JsonSerializer.Deserialize<OpenRouterLlmResponsesResponse>(
                        responseContent,
                        DeserializeOptions);

                    if (body == null)
                        return Result.Failure<OpenRouterLlmResponsesResponse>("Не удалось десериализовать ответ OpenRouter Responses");

                    if (body.Error != null && !string.IsNullOrEmpty(body.Error.Message))
                    {
                        _logger.LogError(
                            "OpenRouter Responses вернул ошибку в теле: {Message}",
                            body.Error.Message);
                        return Result.Failure<OpenRouterLlmResponsesResponse>(body.Error.Message);
                    }

                    if (!string.Equals(body.Status, "completed", StringComparison.OrdinalIgnoreCase))
                    {
                        var msg = $"OpenRouter Responses: статус ответа '{body.Status}'";
                        _logger.LogWarning(msg);
                        return Result.Failure<OpenRouterLlmResponsesResponse>(msg);
                    }

                    _logger.LogInformation("Успешный ответ от OpenRouter Responses API (попытка {Attempt})", attempt);
                    return Result.Success(body);
                }

                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var errorResponse = JsonSerializer.Deserialize<OpenRouterErrorResponse>(
                    errorContent,
                    DeserializeOptions);

                var errorMessage = errorResponse?.Error?.Message ?? $"HTTP {response.StatusCode}: {errorContent}";

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
                lastException = ex;
                _logger.LogWarning(
                    "Таймаут при запросе к OpenRouter Responses API (попытка {Attempt}/{MaxAttempts})",
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
                    "Ошибка сети при запросе к OpenRouter Responses API (попытка {Attempt}/{MaxAttempts})",
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
                    "Неожиданная ошибка при запросе к OpenRouter Responses API (попытка {Attempt}/{MaxAttempts})",
                    attempt,
                    _config.MaxRetryAttempts);
                return Result.Failure<OpenRouterLlmResponsesResponse>($"Исключение при запросе: {ex.Message}");
            }
        }

        return Result.Failure<OpenRouterLlmResponsesResponse>(
            $"Не удалось выполнить запрос OpenRouter Responses после {_config.MaxRetryAttempts} попыток. " +
            $"Последняя ошибка: {lastException?.Message ?? "Неизвестная ошибка"}");
    }
}
