using System.Text.Json.Serialization;

namespace Application.Models.WeekRation;

public sealed class HealthMetricsInterpretationDto
{
    [JsonPropertyName("indicatorMeaningsAndNutritionGoals")]
    public string IndicatorMeaningsAndNutritionGoals { get; set; } = string.Empty;

    [JsonPropertyName("practicalNutritionRecommendations")]
    public string PracticalNutritionRecommendations { get; set; } = string.Empty;

    [JsonPropertyName("allowedProductsExtended")]
    public List<string> AllowedProductsExtended { get; set; } = new();

    [JsonPropertyName("undesirableAndForbiddenProducts")]
    public List<string> UndesirableAndForbiddenProducts { get; set; } = new();
}

/// <summary>
/// Схема ответа OpenRouter <c>json_schema</c> (strict) для расшифровки показателей здоровья.
/// </summary>
public static class HealthMetricsInterpretationJsonSchema
{
    public const string ResponseFormatJson =
        """
        {
          "type": "json_schema",
          "json_schema": {
            "name": "health_metrics_interpretation",
            "strict": true,
            "schema": {
              "type": "object",
              "additionalProperties": false,
              "required": [
                "indicatorMeaningsAndNutritionGoals",
                "practicalNutritionRecommendations",
                "allowedProductsExtended",
                "undesirableAndForbiddenProducts"
              ],
              "properties": {
                "indicatorMeaningsAndNutritionGoals": {
                  "type": "string",
                  "description": "Кратко: что означают показатели и какие цели питания при таких рисках."
                },
                "practicalNutritionRecommendations": {
                  "type": "string",
                  "description": "Практические рекомендации по питанию простым языком."
                },
                "allowedProductsExtended": {
                  "type": "array",
                  "description": "Расширенный список разрешённых продуктов.",
                  "minItems": 1,
                  "items": { "type": "string" }
                },
                "undesirableAndForbiddenProducts": {
                  "type": "array",
                  "description": "Нежелательные и запрещённые продукты; каждый элемент — отдельная позиция.",
                  "minItems": 1,
                  "items": { "type": "string" }
                }
              }
            }
          }
        }
        """;
}
