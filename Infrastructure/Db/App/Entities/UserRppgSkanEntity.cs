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

    /// <summary>
    /// Статус фоновой генерации недельного рациона по этому скану (независимо от <see cref="Status"/>).
    /// </summary>
    public WeekRationGenerationStatus WeekRationGenerationStatus { get; set; } = WeekRationGenerationStatus.None;

    /// <summary>
    /// Текстовое описание этапа генерации рациона (для UI / отладки).
    /// </summary>
    [MaxLength(4000)]
    public string? StatusMessage { get; set; }

    public List<UserRppgScanResultItemEntity> ResultItems { get; set; } = new();
}

public enum RppgScanStatus
{
    New = 0,
    InProgress = 1,
    Failed = 2,
    Completed = 3,
}

/// <summary>Статусы очереди и выполнения генерации рациона по скану RPPG.</summary>
public enum WeekRationGenerationStatus
{
    /// <summary>Генерация не запланирована (например, старые записи до появления флага).</summary>
    None = 0,

    /// <summary>Ожидает обработки воркером.</summary>
    Pending = 1,

    /// <summary>Выполняется (LLM / сохранение).</summary>
    InProgress = 2,

    /// <summary>Рацион успешно сохранён.</summary>
    Completed = 3,

    /// <summary>Не удалось сгенерировать после ретраев.</summary>
    Failed = 4,
}
