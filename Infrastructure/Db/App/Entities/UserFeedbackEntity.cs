using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Db.App.Entities;

/// <summary>
/// Сохранённый JSON-фидбек пользователя.
/// </summary>
public class UserFeedbackEntity : BaseEntity
{
    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    [Comment("Идентификатор пользователя")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Связь с пользователем.
    /// </summary>
    public UserEntity User { get; set; } = null!;

    /// <summary>
    /// Объект фидбека в формате JSON.
    /// </summary>
    [Comment("Объект фидбека в формате JSON")]
    public string? Feedback { get; set; } = "";
}
