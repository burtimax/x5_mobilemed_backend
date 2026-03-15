using System;
using Api.Extensions;
using Api.Middleware;
using Application.Extensions;
using Application.Services.BootstrapDatabase;
using Application.Extensions;
using ModuleTelegramLogger.Extensions;
using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
using Infrastructure.Db.App;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModuleLLM.Extensions;
using NSwag;
using NSwag.Generation.Processors.Security;
using Quartz;
using Serilog;

// Включаем старое поведение timestamp для совместимости с Npgsql
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Настройка конфигурации из различных источников (JSON файлы, переменные окружения)
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Настройка FastEndpoints для обработки API-запросов
builder.Services.AddFastEndpoints(o =>
    {
        o.Assemblies = new[]
        {
            typeof(Program).Assembly,
            //typeof(GetBotsEndpoint).Assembly,
        };
    })
    // Настройка Swagger документации для API
    .SwaggerDocument(o =>
    {
        o.DocumentSettings = s =>
        {
            s.Title = "X5 MobileMed API";
            s.Version = "v1";
            s.OperationProcessors.Add(new OperationSecurityScopeProcessor("Bearer"));
        };
    });

builder.Services.AddHttpClient();

// Настройка ограничения размера файлов для form-data
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104857600; // 100 МБ
});

// Регистрация сервисов приложения
var services = builder.Services;
var config = services.AddConfigurations(builder.Configuration);
services.AddDatabase(config.Database, builder.Environment);
services.AddServices(builder.Configuration);
//services.AddModuleLLM(builder.Configuration);
services.AddCors();
services.AddMapster();
services.AddQuartzHostedService();

// Serilog: консоль + sink для Error+ в Telegram (очередь → Quartz job)
services.AddModuleTelegramLogger(builder.Configuration);
builder.Host.UseSerilog((context, services, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.TelegramErrorSink(services);
});

var app = builder.Build();

// Автоматическое применение миграций базы данных при запуске
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    var databaseBootstrap = scope.ServiceProvider.GetRequiredService<IDatabaseBootstrap>();
    await databaseBootstrap.InitializeAsync();

    // Монтируем задачи по расписанию
    var schedFactory = scope.ServiceProvider.GetRequiredService<ISchedulerFactory>();
    var schedule = await schedFactory.GetScheduler();
    app.ScheduleQuartzJobs(schedule);
}

// Настройка конвейера обработки HTTP-запросов
if (app.Environment.IsDevelopment())
{
    // OpenAPI документация доступна только в режиме разработки
    app.MapOpenApi();
}
else
{
    // В production используем middleware для обработки исключений
    app.UseMiddleware<ResponseExceptionMiddleware>();
}

// Настройка CORS политики
app.UseCors(builder =>
{
    builder.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader();
});

// Подключение аутентификации и авторизации
app.UseAuthentication();
app.UseAuthorization();

// Мидлвар статистики по запросам.
app.UseMiddleware<StatRequestMiddleware>();

// Подключение FastEndpoints и Swagger
app.UseFastEndpoints().UseSwaggerGen();

app.Run();
