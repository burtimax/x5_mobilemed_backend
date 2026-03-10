using System.Text.Json;
using Infrastructure.Db.App.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Models.User;

public class UpdateUserRequest
{
    /// <summary>
    /// Возраст (лет)
    /// </summary>
    public int? Age { get; set; }

    /// <summary>
    /// Рост в см.
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Вес в кг.
    /// </summary>
    public int? Weight { get; set; }

    /// <summary>
    /// Пол пользователя
    /// </summary>
    public Gender? Gender { get; set; }

    /// <summary>
    /// Статус курения
    /// </summary>
    public SmokeStatus? SmokeStatus { get; set; }

    /// <summary>
    /// Цели пользователя
    /// </summary>
    public List<string>? Goals { get; set; }
}
