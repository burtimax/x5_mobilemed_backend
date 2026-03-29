using System.ComponentModel;
using System.Text.Json;

namespace Api.Endpoints.App.SaveUserFeedback;

/// <summary>
/// Запрос на сохранение фидбека пользователя.
/// </summary>
public class SaveUserFeedbackRequest
{
    /// <summary>
    /// Произвольный JSON-объект (или массив) с данными фидбека.
    /// </summary>
    [DefaultValue("{}")]
    public JsonElement Feedback { get; set; }
}
