using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Db.App.Entities;

/// <summary>
/// Зона интерпретации биомаркера — красная, жёлтая или зелёная.
/// </summary>
public class BiomarkerZoneEntity
{
    /// <summary>
    /// Идентификатор зоны.
    /// </summary>
    [Comment("Идентификатор зоны")]
    public int Id { get; set; }

    /// <summary>
    /// Идентификатор шкалы.
    /// </summary>
    [Comment("Идентификатор шкалы")]
    public int BiomarkerScaleId { get; set; }
    public BiomarkerScaleEntity BiomarkerScale { get; set; } = null!;

    /// <summary>
    /// Ключ зоны: red, yellow, green.
    /// </summary>
    [Comment("Ключ зоны (red/yellow/green)")]
    [MaxLength(16)]
    public required string ZoneKey { get; set; }

    /// <summary>
    /// Начало диапазона значения. null — для зон с Rule.
    /// </summary>
    [Comment("Начало диапазона значения")]
    [Precision(12, 4)]
    public decimal? ValueFrom { get; set; }

    /// <summary>
    /// Конец диапазона значения. null — для зон с Rule.
    /// </summary>
    [Comment("Конец диапазона значения")]
    [Precision(12, 4)]
    public decimal? ValueTo { get; set; }

    /// <summary>
    /// Правило интерпретации (например, "value <= age"). Используется для relativeToAge.
    /// </summary>
    [Comment("Правило интерпретации (для relativeToAge)")]
    [MaxLength(128)]
    public string? Rule { get; set; }

    /// <summary>
    /// Комментарий к зоне.
    /// </summary>
    [Comment("Комментарий к зоне")]
    public required string Comment { get; set; }

    /// <summary>
    /// Комментарий к зоне для пользователя.
    /// </summary>
    [Comment("Комментарий к зоне для пользователя")]
    public string? CommentUser { get; set; }
}
