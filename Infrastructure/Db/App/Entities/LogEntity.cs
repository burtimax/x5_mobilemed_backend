using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Db.App.Entities;

/// <summary>
/// Критичный лог приложения (в т.ч. с фронтенда).
/// </summary>
public class LogEntity : BaseEntity
{
    /// <summary>
    /// Идентификатор пользователя (если известен).
    /// </summary>
    [Comment("Идентификатор пользователя (если известен)")]
    public Guid? UserId { get; set; }

    /// <summary>
    /// Связь с пользователем.
    /// </summary>
    public UserEntity? User { get; set; }

    /// <summary>
    /// Источник лога: бэкенд или фронтенд.
    /// </summary>
    [Comment("Источник лога: бэкенд или фронтенд")]
    public LogSource? LogSource { get; set; }

    /// <summary>
    /// Тип/категория лога (произвольная строка).
    /// </summary>
    [Comment("Тип/категория лога")]
    public string? LogType { get; set; }

    /// <summary>
    /// Структурированные данные лога в формате JSON.
    /// </summary>
    [Comment("Структурированные данные лога в формате JSON")]
    public string? Log { get; set; }

    /// <summary>
    /// Текстовое сообщение лога.
    /// </summary>
    [Comment("Текстовое сообщение лога")]
    public string? LogMessage { get; set; }
}
