namespace Application.Models.WeekRation;

/// <summary>
/// Показатели скана, для которых выполняется отдельный LLM-шаг «клиническая расшифровка» перед рационом.
/// </summary>
public static class WeekRationFocusBiomarkerKeys
{
    public static readonly IReadOnlyList<string> All =
    [
        "hemoglobinA1c",
        "highHemoglobinA1CRisk",
        "highFastingGlucoseRisk",
        "bloodPressureSystolic",
        "bloodPressureDiastolic",
        "highBloodPressureRisk",
        "highTotalCholesterolRisk",
        "hemoglobin",
        "lowHemoglobinRisk",
        "ascvdRisk",
        "heartAge"
    ];
}
