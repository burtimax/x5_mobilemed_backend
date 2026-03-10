using System.ComponentModel;

namespace Shared.Models;

public abstract class Pagination
{
    /// <summary>
    /// Номер страницы.
    /// </summary>
    [DefaultValue(1)]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Кол-во элементов на странице.
    /// </summary>
    [DefaultValue(50)]
    public int PageSize { get; set; } = 50;
}
