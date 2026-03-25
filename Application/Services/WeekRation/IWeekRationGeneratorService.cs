using Application.Models.WeekRation;

namespace Application.Services.WeekRation;

public interface IWeekRationGeneratorService
{
    Task<WeekRationGeneratorOutcome> GenerateAsync(
        WeekRationRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);
}
