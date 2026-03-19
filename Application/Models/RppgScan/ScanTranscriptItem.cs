using System.Text.Json.Serialization;

namespace Application.Models.RppgScan;

public class ScanTranscriptItem
{
    public string Key { get; set; }
    /// <summary>
    /// Значение показателя из результата сканирования.
    /// </summary>
    public decimal Value { get; set; }
    public string Color { get; set; }
    public string Name { get; set; }
    public string Unit { get; set; }
    public string DescriptionUser { get; set; }
    public string CommentUser { get; set; }
    public int ConfidenceLevel { get; set; }

    public ScanResultScaleData ScaleMetadata { get; set; }

    /// <summary>
    /// Три ближайшие зоны.
    /// </summary>
    [JsonIgnore]
    public List<ScanTranscriptItemZone> Zones { get; set; }
}
