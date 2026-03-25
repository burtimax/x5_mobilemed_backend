using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModuleLLM.Configuration;
using ModuleLLM.Services;
using Shared.Configs;

namespace ModuleLLM.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddModuleLLM(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var openRouterConfig = configuration.GetSection(OpenRouterApiConfiguration.Section).Get<OpenRouterApiConfiguration>();
        if (openRouterConfig is null)
            throw new Exception("OpenRouter configuration is missing");

        var proxyConfig = configuration.GetSection(ProxyConfiguration.Section).Get<ProxyConfiguration>() ?? new ProxyConfiguration();
        services.AddSingleton(proxyConfig);

        services.AddSingleton(openRouterConfig);
        services.AddTransient<ILlmApiService, OpenRouterService>();
        services.AddTransient<IOpenRouterLlmResponseService, OpenRouterLlmResponseService>();

        return services;
    }
}
