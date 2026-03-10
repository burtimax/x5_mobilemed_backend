using System.Text.Json;

namespace Application.Models.User;

public class UpdateUserRequest
{
    public DateOnly? BirthDate { get; set; }
    public int? Gender { get; set; }
    // Любая структура JSON
    public JsonDocument? Additional { get; set; }
}
