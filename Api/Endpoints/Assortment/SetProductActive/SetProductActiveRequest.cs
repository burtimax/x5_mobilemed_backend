namespace Api.Endpoints.Assortment.SetProductActive;

/// <summary>
/// Запрос на изменение активности товара.
/// </summary>
public class SetProductActiveRequest
{
    /// <summary>
    /// Идентификатор товара (из маршрута).
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Признак активности товара.
    /// </summary>
    public bool IsActive { get; set; }
}
