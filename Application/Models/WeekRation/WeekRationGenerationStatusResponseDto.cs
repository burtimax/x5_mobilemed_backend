using Infrastructure.Db.App.Entities;

namespace Application.Models.WeekRation;

/// <summary>Текущий статус генерации рациона по скану RPPG.</summary>
public sealed class WeekRationGenerationStatusResponseDto
{
    public WeekRationGenerationStatus Status { get; set; }

    public string? StatusMessage { get; set; }
}
