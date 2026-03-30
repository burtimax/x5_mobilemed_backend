namespace Application.Models.RppgScan;

/// <summary>
/// Зона шкалы здоровья. Шкала здоровья представляет собой прямой отрезок от 0% до 100%, у отрезка есть зоны (красные, желтые, зеленые)
/// Нужно шкалу разделить на зоны и указать какой цвет имеет зона (Color) и какую часть шкалы она занимает от (PercentFrom) и до (PercentTo).
/// Пограничные значения зон помечаются числами значений от (From) и до (To)
/// </summary>
public class ScanResultScaleDataItem
{
    public double From { get; set; }
    public double To { get; set; }
    public int PercentFrom { get; set; }
    public int PercentTo { get; set; }
    public string Color { get; set; }
    public string? FromToAlias { get; set; }
    public string? ValueAlias { get; set; }
}
