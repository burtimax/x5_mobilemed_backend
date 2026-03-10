using Infrastructure.Db.App;
using Infrastructure.Db.App.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModuleLLM.Prompt;
using Shared.Const;

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
    }
}
