namespace Application.Models.Auth;

public class UserTokenData
{
    public Guid UserId { get; set; }
    public string ExternalId { get; set; }
    public long SessionId { get; set; }
    public string Utm { get; set; }
}
