using FastEndpoints;

namespace Api.Endpoints.App.HealthCheck;

sealed class HealthCheckRequest
{
    public string? MockMessage { get; set; }
}

sealed class HealthCheckResponse
{
    public string Result { get; set; }
}

sealed class HealthCheckEndpoint : Endpoint<HealthCheckRequest, HealthCheckResponse>
{
    public override void Configure()
    {
        Post("health-check");
        AllowAnonymous();
        Group<AppGroupEndpoints>();
        Summary(s =>
        {
            s.Summary = "Проверка состояния приложения";
            s.Description = $"Если вернула 200, значит приложение в рабочем состоянии. Иначе - ошибка работы приложения или недоступность приложения";
        });
    }

    public override async Task HandleAsync(HealthCheckRequest r, CancellationToken c)
    {
        HealthCheckResponse response = new ()
        {
            Result = "Приложение работоспособно"
        };

        await SendAsync(response, cancellation: c);
    }
}