using System.Globalization;
using System.Security.Cryptography.X509Certificates;

namespace Shared.Const;

public class AppConstants
{
    public class Cultures
    {
        public static CultureInfo Ru = new CultureInfo("ru-RU");
    }

    public class StatEvents
    {
        public const string ApiRequestUnauthorized = "api-req-not-auth"; // Запрос без токена
        public const string ApiRequestAuthorized = "api-req-auth"; // Запрос с токеном
    }
}
