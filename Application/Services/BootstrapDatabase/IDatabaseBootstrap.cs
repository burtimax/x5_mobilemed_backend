namespace Application.Services.BootstrapDatabase;

/// <summary>
/// Сервис заполнения базы данных начальными данными при старте приложения.
/// Вызывается после применения миграций.
/// </summary>
public interface IDatabaseBootstrap
{
    /// <summary>
    /// Инициализирует данные по умолчанию.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
