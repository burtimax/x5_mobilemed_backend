using System.Text.Json;
using Infrastructure.Db.App;
using Infrastructure.Db.App.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Services.BootstrapDatabase;

/// <summary>
/// Заполняет базу данных начальными данными при старте приложения.
/// </summary>
public class DatabaseBootstrap : IDatabaseBootstrap
{
    private readonly AppDbContext _db;
    private readonly ILogger<DatabaseBootstrap> _logger;

    public DatabaseBootstrap(AppDbContext db, ILogger<DatabaseBootstrap> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await SeedBiomarkerZonesAsync(cancellationToken);
    }

    private async Task SeedBiomarkerZonesAsync(CancellationToken cancellationToken)
    {
        if (await _db.Biomarkers.AnyAsync(cancellationToken))
        {
            _logger.LogDebug("Биомаркеры уже заполнены, пропуск сида");
            return;
        }

        var assembly = typeof(BiomarkerEntity).Assembly;
        await using var stream = assembly.GetManifestResourceStream("biomarker_zones_with_descriptions.json");
        if (stream == null)
        {
            _logger.LogWarning("Файл biomarker_zones_with_descriptions.json не найден среди ресурсов");
            return;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var seedData = await JsonSerializer.DeserializeAsync<List<BiomarkerSeedDto>>(stream, options, cancellationToken);
        if (seedData == null || seedData.Count == 0)
        {
            _logger.LogWarning("Нет данных биомаркеров для загрузки");
            return;
        }

        for (var index = 0; index < seedData.Count; index++)
        {
            var dto = seedData[index];
            var biomarker = new BiomarkerEntity
            {
                Key = dto.Key,
                Name = dto.Name,
                Unit = dto.Unit,
                Description = dto.Description,
                DescriptionUser = dto.DescriptionUser,
                Order = dto.Order ?? index
            };

            foreach (var scaleDto in dto.Scales)
            {
                var scale = new BiomarkerScaleEntity
                {
                    Biomarker = biomarker,
                    GenderFrom = (int)scaleDto.GenderInterval.From,
                    GenderTo = (int)scaleDto.GenderInterval.To,
                    WeightFrom = (decimal)scaleDto.WeightInterval.From,
                    WeightTo = (decimal)scaleDto.WeightInterval.To,
                    AgeFrom = (int)scaleDto.AgeInterval.From,
                    AgeTo = (int)scaleDto.AgeInterval.To,
                    ValueFrom = (decimal)scaleDto.ValueIntervals.From,
                    ValueTo = (decimal)scaleDto.ValueIntervals.To,
                    RelativeToAge = scaleDto.RelativeToAge ?? false
                };

                foreach (var zoneDto in scaleDto.Zones)
                {
                    var zone = new BiomarkerZoneEntity
                    {
                        BiomarkerScale = scale,
                        ZoneKey = zoneDto.ZoneKey,
                        ValueFrom = zoneDto.From.HasValue ? (decimal)zoneDto.From.Value : null,
                        ValueTo = zoneDto.To.HasValue ? (decimal)zoneDto.To.Value : null,
                        Rule = zoneDto.Rule,
                        Comment = zoneDto.Comment,
                        CommentUser = zoneDto.CommentUser,
                        FromToAlias = zoneDto.FromToAlias,
                        ValueAlias = zoneDto.ValueAlias
                    };
                    scale.Zones.Add(zone);
                }
                biomarker.Scales.Add(scale);
            }
            _db.Biomarkers.Add(biomarker);
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Загружено биомаркеров: {Count}", seedData.Count);
    }
}
