using System;
using System.Linq;
using System.Security.Claims;
using Application.Models.Auth;
using Microsoft.AspNetCore.Http;

namespace Api.Extensions;

public static class HttpContextExtensions
{
    /// <summary>
    /// Пытается прочитать идентификатор пользователя из JWT без полного набора клеймов
    /// (для публичных эндпоинтов с опциональной авторизацией).
    /// </summary>
    public static bool TryGetUserId(this HttpContext httpContext, out Guid userId)
    {
        userId = default;

        if (httpContext.User?.Identity?.IsAuthenticated != true)
            return false;

        var idClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                      ?? httpContext.User.FindFirst("sub");
        return idClaim?.Value != null && Guid.TryParse(idClaim.Value, out userId);
    }

    public static UserTokenData TokenData(this HttpContext httpContext)
    {
        UserTokenData userTokenData = new();

        string GetClaim(string claim) => httpContext.User.Claims
            ?.First(x => x.Type.Equals(claim, StringComparison.OrdinalIgnoreCase))?.Value ?? throw new Exception($"NOT FOUND CLAIM IN TOKEN [{claim}]");

        userTokenData.UserId = Guid.Parse(GetClaim(ClaimTypes.NameIdentifier));
        userTokenData.SessionId = long.Parse(GetClaim("SessionId"));
        userTokenData.Utm = GetClaim("Utm");

        httpContext.User.Claims.Where(x => x.Type == ClaimTypes.Role)?.ToList();

        return userTokenData;
    }

    public static bool TryGetTokenData(this HttpContext httpContext, out UserTokenData data)
    {
        data = null;

        bool hasAuthHeader = httpContext.Request.Headers.ContainsKey("Authorization");

        if (!hasAuthHeader
            || httpContext.User.Claims == null
            || httpContext.User.Claims.Any() == false) return false;

        data = httpContext.TokenData();
        return true;
    }
}
