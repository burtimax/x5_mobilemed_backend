namespace Application.Models.RppgScan;

/// <summary>
/// Шкала здоровья. Шкала здоровья представляет собой прямой отрезок от 0% до 100%, у отрезка есть зоны (красные, желтые, зеленые)
/// Нужно шкалу разделить на зоны и указать какой цвет имеет зона (Color) и какую часть шкалы она занимает от (PercentFrom) и до (PercentTo).
/// Пограничные значения зон помечаются числами значений от (From) и до (To)
/// </summary>
public class ScanResultScaleData
{
    /// <summary>
    /// Метка значения показателя у пользователя (отмечается на определенной точке шкалы в %)
    /// </summary>
    public int ValuePercentLabel { get; set; }
    /// <summary>
    /// 3 элемента для отображения зон на шкале здоровья
    /// </summary>
    public List<ScanResultScaleDataItem> Items { get; set; }
}
