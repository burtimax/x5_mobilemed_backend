namespace Api.Endpoints.Ration;

/// <summary>Замена основного товара в позиции недельного рациона.</summary>
public sealed class ReplaceWeekRationItemRequest
{
    /// <summary>Идентификатор позиции рациона (<c>WeekRationItemEntity.Id</c>).</summary>
    public Guid Id { get; set; }

    /// <summary>Новый основной товар (PLU / идентификатор каталога).</summary>
    public long ProductId { get; set; }

    /// <summary>Вес порции основной позиции, г (орфография как у сущности: weight).</summary>
    public int Weight { get; set; }
}
