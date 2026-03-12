using System.ComponentModel.DataAnnotations;

namespace Api.Endpoints.App.SaveStatEvent;

/// <summary>
/// Запрос на отправку ошибки на фронте.
/// </summary>
public class SendErrorRequest
{
    public string? ErrorMessage { get; set; }

    public string? Data { get; set; }

    public string? StackTrace { get; set; }
}
