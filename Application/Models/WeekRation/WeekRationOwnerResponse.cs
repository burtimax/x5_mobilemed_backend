using Infrastructure.Db.App.Entities;

namespace Application.Models.WeekRation;

/// <summary>Владелец сохранённого рациона и его продукты-исключения.</summary>
public sealed class WeekRationOwnerResponse
{
    public required UserEntity User { get; init; }

    public required List<UserExcludeProductEntity> ExcludeProducts { get; init; }
}
