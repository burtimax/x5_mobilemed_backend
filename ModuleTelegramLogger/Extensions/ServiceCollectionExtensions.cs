using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModuleTelegramLogger.Configuration;
using Serilog;
using Serilog.Configuration;

namespace ModuleTelegramLogger.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Добавляет модуль отправки ошибок в Telegram: конфигурация, очередь, Serilog sink.
    /// Job (TelegramErrorSenderJob) регистрируется отдельно в Application.
    /// </summary>
    public static IServiceCollection AddModuleTelegramLogger(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(TelegramErrorConfiguration.Section);
        var config = section.Get<TelegramErrorConfiguration>() ?? new TelegramErrorConfiguration();

        if (string.IsNullOrWhiteSpace(config.BotToken))
            config.BotToken = configuration["Bot:TelegramToken"];

        services.AddSingleton(Options.Create(config));
        services.AddSingleton<ITelegramErrorQueue>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<TelegramErrorConfiguration>>();
            return new TelegramErrorQueue(opts.Value.QueueCapacity);
        });
        services.AddSingleton<ITelegramErrorSender, TelegramErrorSender>();

        return services;
    }

    /// <summary>
    /// Регистрирует Serilog sink для отправки Error+ в очередь.
    /// Вызывать внутри UseSerilog(..., (ctx, services, cfg) => { cfg.WriteTo.TelegramErrorSink(services); }).
    /// </summary>
    public static LoggerConfiguration TelegramErrorSink(
        this LoggerSinkConfiguration sinkConfiguration,
        IServiceProvider serviceProvider)
    {
        return sinkConfiguration.Sink(
            new TelegramErrorSerilogSink(
                serviceProvider.GetRequiredService<ITelegramErrorQueue>(),
                serviceProvider.GetRequiredService<IOptions<TelegramErrorConfiguration>>()));
    }
}
