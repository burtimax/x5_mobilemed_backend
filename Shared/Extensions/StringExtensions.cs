using System.Text;
using System.Text.RegularExpressions;

namespace Shared.Extensions;

/// <summary>
/// Расширения для работы со строками
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Преобразует строку из PascalCase или camelCase в snake_case
    /// </summary>
    /// <param name="input">Исходная строка</param>
    /// <returns>Строка в формате snake_case</returns>
    /// <example>
    /// "HelloWorld" -> "hello_world"
    /// "myVariableName" -> "my_variable_name"
    /// </example>
    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var startUnderscores = Regex.Match(input, @"^_+");
        return startUnderscores + Regex.Replace(input, @"([a-z0-9])([A-Z])", "$1_$2").ToLower();
    }

    /// <summary>
    /// Превращает строку в паттерн для поиска с использованием ILIKE в PostgreSQL
    /// </summary>
    /// <remarks>
    /// Из строки "как дела" сделает строку "%как%дела%".
    /// </remarks>
    /// <param name="input">Строка для преобразования</param>
    /// <param name="type">Тип паттерна поиска (начинается с, заканчивается на, содержит)</param>
    /// <returns>Строка-паттерн для ILIKE запроса</returns>
    public static string ToILikePattern(this string? input, ILikePatternType type = ILikePatternType.StartsWith)
    {
        if (input == null) return "";
        input = input.Trim(' ');

        if (string.IsNullOrEmpty(input)) return "";

        var words = input.Split(' ');
        StringBuilder sb = new();

        sb.Append("%");
        foreach (var word in words)
        {
            sb.Append(word + "%");
        }

        string resultPattern = sb.ToString();

        return type switch
        {
            ILikePatternType.StartsWith => resultPattern.TrimStart('%'),
            ILikePatternType.EndsWith => resultPattern.TrimEnd('%'),
            _ => resultPattern
        };
    }

    /// <summary>
    /// Тип паттерна для ILIKE поиска
    /// </summary>
    public enum ILikePatternType
    {
        /// <summary>
        /// Строка начинается с паттерна
        /// </summary>
        StartsWith = 1,

        /// <summary>
        /// Строка заканчивается паттерном
        /// </summary>
        EndsWith = 2,

        /// <summary>
        /// Строка содержит паттерн
        /// </summary>
        Contains = 3,
    }

    /// <summary>
    /// Форматирование строки с использованием параметров
    /// </summary>
    /// <param name="str">Строка-шаблон</param>
    /// <param name="args">Аргументы для форматирования</param>
    /// <returns>Отформатированная строка</returns>
    /// <example>
    /// "Hello {0}, you are {1} years old".F("John", 25) -> "Hello John, you are 25 years old"
    /// </example>
    public static string F(this string str, params object[] args)
    {
        return string.Format(str, args);
    }

    private static readonly Regex HtmlFragmentsRegex = new Regex(
        @"<([a-z][a-z0-9]*)\b[^>]*>.*?</\1>|<([a-z][a-z0-9]*)\b[^>]*/?>",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);

    /// <summary>
    /// Возвращает конкатенацию всех найденных HTML-фрагментов. Если ничего не найдено — null.
    /// </summary>
    public static string? ExtractHtml(this string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var matches = HtmlFragmentsRegex.Matches(input);
        if (matches.Count == 0) return null;

        return string.Concat(matches.Cast<Match>().Select(m => m.Value));
    }
}
