using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Infrastructure.Db.App.Entities;

public class UserRppgScanEntity : BaseEntity
{
    /// <summary>
    /// Идентификатор пользователя
    /// </summary>
    public Guid UserId { get; set; }
    [JsonIgnore]
    public UserEntity? User { get; set; }

    /// <summary>
    /// Результаты сканирования от SDK (JSON)
    /// </summary>
    public string? SdkResult { get; set; }

    public RppgScanStatus Status { get; set; } = RppgScanStatus.New;

    public List<UserRppgScanResultItemEntity> ResultItems { get; set; } = new();
}

public enum RppgScanStatus
{
    New = 0,
    InProgress = 1,
    Failed = 2,
    Completed = 3,
}
