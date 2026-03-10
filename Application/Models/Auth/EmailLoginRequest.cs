namespace Application.Models.Auth;

/// <summary>
/// Запрос на авторизацию пользователя по email и паролю
/// </summary>
public sealed class LoginRequest
{
    /// <summary>
    /// Внешний ID пользователя
    /// </summary>
    public Guid? Id { get; set; } = null!;

    /// <summary>
    /// UTM метка (опционально)
    /// </summary>
    public string? Utm { get; set; }
}

