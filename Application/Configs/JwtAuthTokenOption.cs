namespace Application.Configs
{
    public class JwtAuthTokenOption
    {
        public string? SecretKey { get; set; }
        public int ExpiryMinutes { get; set; }
        public string? Issuer { get; set; }
    }
}
