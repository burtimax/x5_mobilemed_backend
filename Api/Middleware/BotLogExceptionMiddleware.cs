using System.Text;
using Api.Extensions;
using Application.Extensions;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Shared.Const;
using Shared.Contracts;

namespace Api.Middleware;

public class BotLogExceptionMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<BotLogExceptionMiddleware> _logger;

    public BotLogExceptionMiddleware(RequestDelegate next, ILogger<BotLogExceptionMiddleware> logger)
    {
        this.next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next.Invoke(context);
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            await context.Response.SendAsync(Result.Failure(e.Message), StatusCodes.Status500InternalServerError);
        }
    }
}
