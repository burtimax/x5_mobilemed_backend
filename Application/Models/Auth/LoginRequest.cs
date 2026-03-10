namespace Application.Models.Auth
{
    sealed public class TelegramLoginRequest
    {
        public long BotId { get; set; }
        public long UserTelegramId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? UserName { get; set; }
        public string? PhotoUrl { get; set; }
        public string InitData { get; set; }
        public string? Utm { get; set; }

        /// <summary>
        /// Для теста нужно это поле
        /// </summary>
        public bool? IgnoreValidate { get; set; }
    }
}
