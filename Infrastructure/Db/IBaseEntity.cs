namespace Infrastructure.Db;

/// <summary>
/// Поля базовой сущности.
/// </summary>
public interface IBaseEntity
{
    /// <summary>
    /// Когда сущность была создана.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Кто создал сущность.
    /// </summary>
    public Guid? CreatedById { get; set; }

    /// <summary>
    /// Когда сущность была в последний раз обновлена.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Кто обновил сущность.
    /// </summary>
    public Guid? UpdatedById { get; set; }

    /// <summary>
    /// Когда сущность была удалена.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// Кто удалил сущность.
    /// </summary>
    public Guid? DeletedById { get; set; }

}
