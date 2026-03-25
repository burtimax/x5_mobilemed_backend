using Infrastructure.Db.App.Entities;

namespace Application.Services.WeekRation;

internal static class DailyEnergyEstimate
{
    /// <summary>Mifflin–St Jeor BMR × 1,375 (лёгкая активность), целые ккал.</summary>
    public static int? EstimateMaintenanceKcal(int? age, Gender? gender, int? weightKg, int? heightCm)
    {
        if (age is < 14 or > 100 || weightKg is < 35 or > 250 || heightCm is < 120 or > 230 || gender is null)
            return null;

        var w = weightKg!.Value;
        var h = heightCm!.Value;
        var a = age!.Value;
        var bmr = gender.Value == Gender.Female
            ? 10 * w + 6.25m * h - 5 * a - 161
            : 10 * w + 6.25m * h - 5 * a + 5;
        var tdee = bmr * 1.375m;
        return (int)decimal.Round(tdee, 0, MidpointRounding.AwayFromZero);
    }
}
