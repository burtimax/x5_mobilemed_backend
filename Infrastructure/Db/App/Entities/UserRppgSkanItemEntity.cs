using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Infrastructure.Db.App.Entities;

public class UserRppgScanResultItemEntity : BaseEntity
{
    public Guid ScanId { get; set; }
    [JsonIgnore]
    public UserRppgScanEntity? Scan { get; set; }

    /// <summary>
    /// Ключ показателя. Например, pulseRate, respirationRate ...
    /// </summary>
    [MaxLength(30)]
    public string Key { get; set; }

    /// <summary>
    /// Значение показателя
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Уровень уверенности скана показателя
    /// </summary>
    public int? ConfidenceLevel { get; set; }

    /// <summary>
    /// Единица измерения значения
    /// </summary>
    [MaxLength(30)]
    public string? Unit { get; set; }
}



