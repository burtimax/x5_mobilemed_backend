namespace Application.Models.User;

public class CreateUserRequest
{
    public string? UserName { get; set; }
    public string FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public DateOnly? BirthDate { get; set; }
    public int? Gender { get; set; }
    public string? PhotoUrl { get; set; }
    public string? ClinicRole { get; set; }
    public string? Specialization { get; set; }
}