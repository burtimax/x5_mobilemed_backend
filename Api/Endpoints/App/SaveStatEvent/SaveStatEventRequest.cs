using System.ComponentModel.DataAnnotations;

namespace Api.Endpoints.App.SaveStatEvent;

/// <summary>
/// Запрос на сохранение статистического события
/// </summary>
public class SaveStatEventRequest
{
    /// <summary>
    /// Тип события (максимум 30 символов)
    /// </summary>
    [MaxLength(30)]
    public string? Type { get; set; }

    /// <summary>
    /// Данные события (максимум 100 символов)
    /// </summary>
    [MaxLength(100)]
    public string? Data { get; set; }

    /// <summary>
    /// Длительность события
    /// </summary>
    public double? DurationSeconds { get; set; }
}
