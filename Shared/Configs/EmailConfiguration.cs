namespace Shared.Configs;

public class EmailConfiguration
{
    public const string Section = "EmailService";
    public string Name { get; set; }
    public string EmailAddress { get; set; }
    public string Password { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
}
