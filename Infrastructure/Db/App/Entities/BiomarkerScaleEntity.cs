using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Db.App.Entities;

/// <summary>
/// Шкала интерпретации биомаркера — диапазоны пола, возраста, веса и допустимых значений.
/// </summary>
public class BiomarkerScaleEntity
{
    /// <summary>
    /// Идентификатор шкалы.
    /// </summary>
    [Comment("Идентификатор шкалы")]
    public int Id { get; set; }

    /// <summary>
    /// Идентификатор биомаркера.
    /// </summary>
    [Comment("Идентификатор биомаркера")]
    public int BiomarkerId { get; set; }
    public BiomarkerEntity Biomarker { get; set; } = null!;

    /// <summary>
    /// Пол: 0 — женщина, 1 — мужчина. Интервал [From, To] включительно.
    /// </summary>
    [Comment("Пол от (0=женщина, 1=мужчина)")]
    public int GenderFrom { get; set; }

    [Comment("Пол до (0=женщина, 1=мужчина)")]
    public int GenderTo { get; set; }

    /// <summary>
    /// Диапазон веса в кг.
    /// </summary>
    [Comment("Вес от (кг)")]
    [Precision(8, 2)]
    public decimal WeightFrom { get; set; }

    [Comment("Вес до (кг)")]
    [Precision(8, 2)]
    public decimal WeightTo { get; set; }

    /// <summary>
    /// Диапазон возраста в годах.
    /// </summary>
    [Comment("Возраст от (лет)")]
    public int AgeFrom { get; set; }

    [Comment("Возраст до (лет)")]
    public int AgeTo { get; set; }

    /// <summary>
    /// Диапазон допустимых значений параметра.
    /// </summary>
    [Comment("Значение параметра от")]
    [Precision(12, 4)]
    public decimal ValueFrom { get; set; }

    [Comment("Значение параметра до")]
    [Precision(12, 4)]
    public decimal ValueTo { get; set; }

    /// <summary>
    /// Интерпретация значения относительно возраста пользователя (например, heartAge).
    /// </summary>
    [Comment("Интерпретация относительно возраста пользователя")]
    public bool RelativeToAge { get; set; }

    /// <summary>
    /// Зоны интерпретации (зелёная, жёлтая, красная).
    /// </summary>
    public ICollection<BiomarkerZoneEntity> Zones { get; set; } = [];
}
