using Application.Models.WeekRation;

namespace Application.Services.WeekRation;

public interface IWeekRationPersistenceService
{
    Task<Guid> SaveNewRationAsync(
        Guid userId,
        Guid rppgScanId,
        IReadOnlyList<DayRationMealSlotDto> slots,
        CancellationToken cancellationToken = default);

    Task DeleteOtherRationsForScanAsync(
        Guid rppgScanId,
        Guid keepRationId,
        CancellationToken cancellationToken = default);
}
