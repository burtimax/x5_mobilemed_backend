using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Db.App.Entities;

/// <summary>
/// Биомаркер — параметр здоровья с описаниями.
/// </summary>
public class BiomarkerEntity
{
    /// <summary>
    /// Идентификатор биомаркера.
    /// </summary>
    [Comment("Идентификатор биомаркера")]
    public int Id { get; set; }

    /// <summary>
    /// Уникальный ключ биомаркера (например, pulseRate, oxygenSaturation).
    /// </summary>
    [Comment("Уникальный ключ биомаркера")]
    [MaxLength(64)]
    public required string Key { get; set; }

    /// <summary>
    /// Название показателя.
    /// </summary>
    [Comment("Название показателя")]
    [MaxLength(128)]
    public required string Name { get; set; }

    /// <summary>
    /// Единица измерения.
    /// </summary>
    [Comment("Единица измерения")]
    [MaxLength(32)]
    public string? Unit { get; set; }

    /// <summary>
    /// Порядок отображения (меньше — выше в списке).
    /// </summary>
    [Comment("Порядок отображения")]
    public int Order { get; set; }

    /// <summary>
    /// Учитывать показатель при отображении скана (распознавание, отчёты, транскрипты для клиента).
    /// </summary>
    [Comment("Активен для отображения в сканах")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Техническое описание параметра.
    /// </summary>
    [Comment("Техническое описание параметра")]
    public required string Description { get; set; }

    /// <summary>
    /// Описание для пользователя.
    /// </summary>
    [Comment("Описание для пользователя")]
    public required string DescriptionUser { get; set; }

    /// <summary>
    /// Шкалы интерпретации значений (по полу, возрасту, весу).
    /// </summary>
    public ICollection<BiomarkerScaleEntity> Scales { get; set; } = [];
}
