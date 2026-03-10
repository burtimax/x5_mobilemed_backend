using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Db.App;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        var connectionString = "";

        // Если всё ещё не найдено, используем значение по умолчанию
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = "Host=127.0.0.1;Port=5432;Database=x5_mobilemed_db;Username=postgres;Password=123;Include Error Detail=true";
        }

        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
