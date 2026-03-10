using System.Text.Json.Serialization;

namespace Application.Services.Llm;

public class ConsultationResultModel
{
    [JsonPropertyName("complaints")]
    public string? Complaints { get; set; }

    [JsonPropertyName("objective")]
    public string? Objective { get; set; }

    [JsonPropertyName("treatment_plan")]
    public string? TreatmentPlan { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }
}
