using System;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts;
using Shared.Extensions;
using Shared.Models;

namespace Api.Middleware;

/// <summary>
/// Middleware для глобальной обработки необработанных исключений
/// </summary>
public class ResponseExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// Инициализирует новый экземпляр ExceptionMiddleware
    /// </summary>
    /// <param name="next">Следующий middleware в пайплайне</param>
    /// <param name="logger">Логгер для записи ошибок</param>
    /// <param name="environment">Информация об окружении приложения</param>
    public ResponseExceptionMiddleware(
        RequestDelegate next,
        ILogger<ResponseExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Обрабатывает HTTP запрос и перехватывает необработанные исключения
    /// </summary>
    /// <param name="context">HTTP контекст</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next.Invoke(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Обрабатывает перехваченное исключение и формирует ответ клиенту
    /// </summary>
    /// <param name="context">HTTP контекст</param>
    /// <param name="exception">Перехваченное исключение</param>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Логируем полную информацию об ошибке
        _logger.LogError(exception,
            "Необработанное исключение при обработке запроса {Method} {Path}. TraceId: {TraceId}",
            context.Request.Method,
            context.Request.Path,
            context.TraceIdentifier);

        // В development возвращаем детальную информацию об ошибке
        // В production возвращаем только общее сообщение (для безопасности)
        var errorMessage = _environment.IsDevelopment()
            ? exception.Message.ToString()
            : "Произошла внутренняя ошибка сервера. Пожалуйста, обратитесь к администратору.";

        if (exception is GatewayException)
        {
            errorMessage = exception.Message;
        }

        // var errorDetail = new
        // {
        //     message = exception.Message,
        //     stackTrace = exception.StackTrace,
        //     innerException = exception.InnerException?.Message
        // };

        var errorDetail = _environment.IsDevelopment()
            ? new
            {
                message = exception.Message,
                stackTrace = exception.StackTrace,
                innerException = exception.InnerException?.Message
            }
            : null;

        await context.Response.SendAsync(
            Result.Failure(errorDetail?.ToJson() ?? errorMessage),
            StatusCodes.Status500InternalServerError);
    }
}
