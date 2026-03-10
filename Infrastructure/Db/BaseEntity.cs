using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Infrastructure.Db.App.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Db;

/// <summary>
/// Базовая сущность БД.
/// </summary>
public class BaseEntity : IBaseEntity
{
    /// <summary>
    /// ИД сущности.
    /// </summary>
    [Comment("ИД сущности.")]
    [JsonPropertyOrder(int.MinValue)]
    public Guid Id { get; set; }

    /// <summary>
    /// Когда сущность была создана.
    /// </summary>
    [Comment("Когда сущность была создана.")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Кто создал сущность.
    /// </summary>
    [Comment("Кто создал сущность.")]
    [JsonIgnore]
    public Guid? CreatedById { get; set; }
    [JsonIgnore]
    public UserEntity? CreatedBy { get; set; }

    /// <summary>
    /// Когда сущность была в последний раз обновлена.
    /// </summary>
    [Comment("Когда сущность была в последний раз обновлена.")]
    [JsonIgnore]
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Кто обновил сущность.
    /// </summary>
    [Comment("Кто обновил сущность.")]
    [JsonIgnore]
    public Guid? UpdatedById { get; set; }
    [JsonIgnore]
    public UserEntity? UpdatedBy { get; set; }


    /// <summary>
    /// Когда сущность была удалена.
    /// </summary>
    [Comment("Когда сущность была удалена.")]
    [JsonIgnore]
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// Кто удалил сущность.
    /// </summary>
    [Comment("Кто удалил сущность.")]
    [JsonIgnore]
    public Guid? DeletedById { get; set; }
    [JsonIgnore]
    public UserEntity? DeletedBy { get; set; }
}
