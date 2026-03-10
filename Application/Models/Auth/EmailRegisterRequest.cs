namespace Application.Models.Auth;

/// <summary>
/// Запрос на регистрацию пользователя по email и паролю
/// </summary>
public sealed class EmailRegisterRequest
{
    /// <summary>
    /// Email пользователя
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// Пароль пользователя
    /// </summary>
    public string Password { get; set; } = null!;

    /// <summary>
    /// Имя пользователя
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Отчество пользователя
    /// </summary>
    public string? MiddleName { get; set; }

    /// <summary>
    /// Фамилия пользователя
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Дата рождения пользователя
    /// </summary>
    public DateOnly? BirthDate { get; set; }

    /// <summary>
    /// Пол пользователя
    /// </summary>
    public int? Gender { get; set; }

    /// <summary>
    /// Роль пользователя в клинике: врач, координатор...
    /// </summary>
    public string? ClinicRole { get; set; }

    /// <summary>
    /// Специализация пользователя: врач-терапевт и т.д...
    /// </summary>
    public string? Specialization { get; set; }
}

