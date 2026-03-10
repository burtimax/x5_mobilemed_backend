namespace Shared.Models;

public interface IOrdered
{
    /// <summary>
    /// Порядок сортировки.
    /// </summary>
    /// <remarks>
    /// Формат строки: "+PropertyName1,-PropertyName2,+PropertyName3"
    /// </remarks>
    public string? Order { get; set; }
}