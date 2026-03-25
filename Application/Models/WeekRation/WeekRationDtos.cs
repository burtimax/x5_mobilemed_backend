using System.Text.Json.Serialization;
using Infrastructure.Db.App.Entities;

namespace Application.Models.WeekRation;

/// <summary>Элемент рациона: id из ответа LLM и карточка товара из БД после <see cref="WeekRationEnrichment"/>.</summary>
public sealed class WeekRationProductRefDto
{
    /// <summary>Идентификатор товара (как вернул LLM).</summary>
    public long Id { get; set; }

    /// <summary>Товар из каталога; заполняется на сервере, если запись найдена по <see cref="Id"/>.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ProductEntity? Product { get; set; }
}

/// <summary>Один день недельного рациона: приёмы пищи — списки товаров с id.</summary>
public sealed class WeekRationDayDto
{
    /// <summary>Номер дня от 1 до 7.</summary>
    public int Day { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<WeekRationProductRefDto>? Breakfast { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<WeekRationProductRefDto>? Lunch { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<WeekRationProductRefDto>? Snack { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<WeekRationProductRefDto>? Dinner { get; set; }
}
