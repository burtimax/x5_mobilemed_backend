namespace Application.Models.RppgScan;

public class ScanTranscriptItem
{
    public string Key { get; set; }
    public string Name { get; set; }
    public string Unit { get; set; }
    public string DescriptionUser { get; set; }
    public int ConfidenceLevel { get; set; }
    /// <summary>
    /// Три ближайшие зоны.
    /// </summary>
    public List<ScanTranscriptItemZone> Zones { get; set; }
}
