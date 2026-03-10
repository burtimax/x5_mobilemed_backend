using System;
using Application.Extensions;
using Application.Services.BootstrapDatabase;
using Application.Services.Email;
using Application.Services.Llm;
using Application.Services.StatEvent;
using Application.Services.User;
using Application.Utils;
using Infrastructure.Db.App;
using Infrastructure.Db.App.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Shared.Configs;

namespace Api.Extensions;

public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Добавляет планировщик задач Quartz для рассылок.
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    public static void AddQuartzHostedService(this IServiceCollection services)
    {
        services.AddQuartz(quartzConfigurator =>
        {
            quartzConfigurator.UseMicrosoftDependencyInjectionJobFactory();
        });
        services.AddQuartzHostedService(options => { options.WaitForJobsToComplete = true; });
    }



    public static void AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddJwt(configuration);
        services.AddScoped<IUserService, UserService>();

        services.AddScoped<IStatEventService, StatEventService>();
        services.AddScoped<IDatabaseBootstrap, DatabaseBootstrap>();

        services.AddScoped<ILlmService, LlmService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
        services.AddScoped<SetByUserIdInterceptor>();
    }

    public static AppConfiguration AddConfigurations(this IServiceCollection services, IConfiguration configuration)
    {
        AppConfiguration config = configuration.Get<AppConfiguration>();
        if(config == null) throw new NullReferenceException(nameof(config));
        services.AddSingleton(config);

        // Регистрация FileUploadConfiguration
        var fileUploadConfig = configuration.GetSection(FileUploadConfiguration.Section).Get<FileUploadConfiguration>();
        if(fileUploadConfig == null) throw new NullReferenceException(nameof(FileUploadConfiguration));
        services.AddSingleton(fileUploadConfig);

        // Регистрация ProxyConfiguration
        var proxyConfig = configuration.GetSection(ProxyConfiguration.Section).Get<ProxyConfiguration>();
        if(proxyConfig == null) proxyConfig = new ProxyConfiguration(); // Опциональная конфигурация
        services.AddSingleton(proxyConfig);

        return config;
    }

    /// <summary>
    /// Регистрирует контекст базы данных в DI контейнере
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    /// <param name="config">Конфигурация подключения к БД</param>
    /// <param name="environment">Информация об окружении приложения</param>
    public static void AddDatabase(
        this IServiceCollection services,
        DatabaseAppConfiguration config,
        IHostEnvironment environment)
    {
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            {
                options.UseNpgsql(config.AppDbConnection);
                options.AddInterceptors(serviceProvider.GetRequiredService<SetByUserIdInterceptor>());
                // ВАЖНО: EnableSensitiveDataLogging включается только в режиме разработки
                // В production это может привести к утечке конфиденциальных данных в логах
                if (environment.IsDevelopment())
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
            }
        );
    }
}
