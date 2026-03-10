using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModuleLLM.Configuration;
using ModuleLLM.Services;

namespace ModuleLLM.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddModuleLLM(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var groqConfig = configuration.GetSection("GroqApi").Get<GroqApiConfiguration>();
        if (groqConfig is null) throw new Exception("GroqApi configuration is missing");

        services.AddSingleton(groqConfig);
        services.Configure<LlmProviderOptions>(configuration.GetSection(LlmProviderOptions.Section));

        var routerAiConfig = configuration.GetSection(RouterAIApiConfiguration.Section).Get<RouterAIApiConfiguration>();
        var gigaChatConfig = configuration.GetSection(GigaChatApiConfiguration.Section).Get<GigaChatApiConfiguration>();
        var llmOptions = configuration.GetSection(LlmProviderOptions.Section).Get<LlmProviderOptions>()
            ?? new LlmProviderOptions();

        if (string.Equals(llmOptions.Provider, "router-ai", StringComparison.OrdinalIgnoreCase))
        {
            if (routerAiConfig is null)
                throw new Exception("RouterAI configuration is required when Llm:Provider is router-ai");
            services.AddSingleton(routerAiConfig);
            services.AddTransient<ILlmApiService, RouterAIApiService>();
        }
        else if (string.Equals(llmOptions.Provider, "gigachat", StringComparison.OrdinalIgnoreCase))
        {
            if (gigaChatConfig is null)
                throw new Exception("GigaChat configuration is required when Llm:Provider is gigachat");
            services.AddSingleton(gigaChatConfig);
            services.AddTransient<ILlmApiService, GigaChatApiService>();
        }
        else
        {
            services.AddTransient<ILlmApiService, GroqApiService>();
        }

        return services;
    }
}

