using System.ComponentModel;
using System.Text.Json;

namespace Api.Endpoints.App.SaveFrontendLog;

/// <summary>
/// Запрос на сохранение лога с фронтенда.
/// </summary>
public class SaveFrontendLogRequest
{
    /// <summary>Тип или категория события.</summary>
    public string? LogType { get; set; }

    /// <summary>Структурированные данные (произвольный JSON).</summary>
    [DefaultValue("{}")]
    public JsonElement Log { get; set; }

    /// <summary>Текстовое сообщение.</summary>
    public string? LogMessage { get; set; }
}
