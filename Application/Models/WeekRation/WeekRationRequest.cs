namespace Application.Models.WeekRation;

public sealed class WeekRationRequest
{
    /// <summary>Идентификатор сохранённого скана RPPG (результата сканирования).</summary>
    public Guid ScanId { get; set; }

    public string? Model { get; set; }

    public double? Temperature { get; set; }

    public int? MaxTokens { get; set; }

    public double? TopP { get; set; }
}
