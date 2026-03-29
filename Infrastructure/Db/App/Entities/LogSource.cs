namespace Infrastructure.Db.App.Entities;

/// <summary>
/// Источник записи критичного лога (бэкенд или фронтенд).
/// </summary>
public enum LogSource
{
    Backend = 0,
    Frontend = 1,
}
