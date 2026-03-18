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
