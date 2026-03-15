using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Db.App.Entities;

/// <summary>
/// Товар X5.
/// </summary>
public class ProductEntity
{
    /// <summary>
    /// Идентификатор товара.
    /// </summary>
    [Comment("Идентификатор товара")]
    public long Id { get; set; }

    /// <summary>
    /// Идентификатор категории.
    /// </summary>
    [Comment("Идентификатор категории")]
    public int CategoryId { get; set; }

    /// <summary>
    /// Категория товара.
    /// </summary>
    public CategoryEntity Category { get; set; } = null!;

    /// <summary>
    /// PLU (Price Look-Up) код товара.
    /// </summary>
    [Comment("PLU код товара")]
    [MaxLength(32)]
    public required string Plu { get; set; }

    /// <summary>
    /// Название товара.
    /// </summary>
    [Comment("Название товара")]
    public required string Title { get; set; }

    /// <summary>
    /// URL изображений товара.
    /// </summary>
    [Comment("URL изображений товара")]
    public List<string> Images { get; set; } = [];

    /// <summary>
    /// Метки товара.
    /// </summary>
    [Comment("Метки товара")]
    public string? Labels { get; set; }

    /// <summary>
    /// Рейтинг товара.
    /// </summary>
    [Comment("Рейтинг товара")]
    public int? Rating { get; set; }

    /// <summary>
    /// Ккал на 100 г.
    /// </summary>
    [Comment("Ккал на 100 г")]
    [Precision(10, 2)]
    public decimal? KcalPer100G { get; set; }

    /// <summary>
    /// Белки на 100 г (г).
    /// </summary>
    [Comment("Белки на 100 г")]
    [Precision(10, 2)]
    public decimal? ProteinsGPer100G { get; set; }

    /// <summary>
    /// Жиры на 100 г (г).
    /// </summary>
    [Comment("Жиры на 100 г")]
    [Precision(10, 2)]
    public decimal? FatsGPer100G { get; set; }

    /// <summary>
    /// Углеводы на 100 г (г).
    /// </summary>
    [Comment("Углеводы на 100 г")]
    [Precision(10, 2)]
    public decimal? CarbsGPer100G { get; set; }

    /// <summary>
    /// Аллергены.
    /// </summary>
    [Comment("Аллергены")]
    public string? Allergens { get; set; }

    /// <summary>
    /// Основные ингредиенты.
    /// </summary>
    [Comment("Основные ингредиенты")]
    public string? MainIngrediants { get; set; }

    /// <summary>
    /// Полный состав.
    /// </summary>
    [Comment("Полный состав")]
    public string? FullIngrediants { get; set; }

    /// <summary>
    /// Характеристики товара (ключ, название, отображаемое значение).
    /// </summary>
    [Comment("Характеристики товара")]
    public List<ProductFeatureDto> Features { get; set; } = [];

    /// <summary>
    /// Цена в копейках.
    /// </summary>
    [Comment("Цена в копейках")]
    public int? Price { get; set; }

    /// <summary>
    /// Тип продукта.
    /// </summary>
    [Comment("Тип продукта")]
    public string? ProductType { get; set; }

    /// <summary>
    /// Производитель.
    /// </summary>
    [Comment("Производитель")]
    public string? Manufacturer { get; set; }

    /// <summary>
    /// Бренд.
    /// </summary>
    [Comment("Бренд")]
    public string? Brand { get; set; }

    /// <summary>
    /// Страна производства.
    /// </summary>
    [Comment("Страна производства")]
    public string? Country { get; set; }

    /// <summary>
    /// Срок годности в днях.
    /// </summary>
    [Comment("Срок годности в днях")]
    public int? ShelfLifeDays { get; set; }

    /// <summary>
    /// Вес в граммах.
    /// </summary>
    [Comment("Вес в граммах")]
    public int? WeightG { get; set; }

    /// <summary>
    /// Наименование единицы измерения.
    /// </summary>
    [Comment("Наименование единицы измерения")]
    [MaxLength(32)]
    public string? UnitName { get; set; }

    /// <summary>
    /// Объём в мл.
    /// </summary>
    [Comment("Объём в мл")]
    [Precision(14, 3)]
    public decimal? VolumeMl { get; set; }

    /// <summary>
    /// Содержит алкоголь.
    /// </summary>
    [Comment("Содержит алкоголь")]
    public bool? IsAlcohol { get; set; }

    /// <summary>
    /// Табачное изделие.
    /// </summary>
    [Comment("Табачное изделие")]
    public bool? IsTobacco { get; set; }

    /// <summary>
    /// Контент 18+.
    /// </summary>
    [Comment("Контент 18+")]
    public bool? IsAdultContent { get; set; }

    /// <summary>
    /// Приоритет сортировки.
    /// </summary>
    [Comment("Приоритет сортировки")]
    public int Priority { get; set; }

    /// <summary>
    /// Активен ли товар.
    /// </summary>
    [Comment("Активен ли товар")]
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Характеристика товара (для JSON-колонки features).
/// </summary>
public class ProductFeatureDto
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("displayValues")]
    public string DisplayValues { get; set; } = string.Empty;
}
