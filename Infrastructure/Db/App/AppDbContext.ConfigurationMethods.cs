using System.Linq.Expressions;
using Infrastructure.Db.App.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Shared.Extensions;

namespace Infrastructure.Db.App;

public partial class AppDbContext
{
    /// <summary>
    /// Таблицы, свойства, ключи, внеш. ключи, индексы переводит в нижний регистр в БД.
    /// </summary>
    protected void SetAllToSnakeCase(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            entityType.SetTableName(entityType.GetTableName().ToSnakeCase());

            foreach (var property in entityType.GetProperties())
            {
                var schema = entityType.GetSchema();
                var tableName = entityType.GetTableName();
                var storeObjectIdentifier = StoreObjectIdentifier.Table(tableName, schema);
                property.SetColumnName(property.GetColumnName(storeObjectIdentifier).ToSnakeCase());
            }

            foreach (var key in entityType.GetKeys())
                key.SetName(key.GetName().ToSnakeCase());

            foreach (var key in entityType.GetForeignKeys())
                key.SetConstraintName(key.GetConstraintName().ToSnakeCase());

            foreach (var index in entityType.GetIndexes())
                index.SetDatabaseName(index.GetDatabaseName().ToSnakeCase());
        }
    }

    /// <summary>
    /// Задать наименование таблиц и схемы для таблиц.
    /// </summary>
    private void SetSchemasToTables(ModelBuilder builder)
    {
        // Identity таблицы
        builder.Entity<UserEntity>().ToTable("users", appSchema);
        builder.Entity<UserProfileEntity>().ToTable("user_profiles", appSchema);

        // Прикладные таблицы
        builder.Entity<StatEventEntity>().ToTable("stat_events", statSchema);
        builder.Entity<UserRppgScanEntity>().ToTable("user_rppg_scans", appSchema);
        builder.Entity<UserRppgScanResultItemEntity>().ToTable("user_rppg_scan_result_items", appSchema);
    }

    /// <summary>
    /// Настройка фильтров запросов.
    /// </summary>
    public static void SetFilters(ModelBuilder modelBuilder)
    {
        // Фильтр для UserEntity (наследуется от IdentityUser, но реализует IBaseEntity)
        modelBuilder.Entity<UserEntity>()
            .HasQueryFilter(e => e.DeletedAt == null);

        // Фильтр для остальных сущностей, наследующих от BaseEntity
        var entities = modelBuilder.Model
            .GetEntityTypes()
            .Where(e => e.ClrType.BaseType == typeof(BaseEntity))
            .Select(e => e.ClrType);

        Expression<Func<BaseEntity, bool>>
            expression = del => del.DeletedAt == null;

        foreach (var e in entities)
        {
            ParameterExpression p = Expression.Parameter(e);
            Expression body =
                ReplacingExpressionVisitor
                    .Replace(expression.Parameters.Single(),
                        p, expression.Body);

            modelBuilder.Entity(e)
                .HasQueryFilter(
                    Expression.Lambda(body, p));
        }
    }

    public static void ConfigureEntities(ModelBuilder builder)
    {
        // Настройка связи UserEntity -> UserProfileEntity (1:1)
        builder.Entity<UserEntity>()
            .HasOne(u => u.Profile)
            .WithOne(p => p.User)
            .HasForeignKey<UserProfileEntity>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Настройка связи StatEventEntity -> UserEntity
        builder.Entity<StatEventEntity>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Настройка связи UserRppgScanEntity -> UserEntity
        builder.Entity<UserRppgScanEntity>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Настройка связи UserRppgScanResultItemEntity -> UserRppgScanEntity
        builder.Entity<UserRppgScanResultItemEntity>()
            .HasOne(i => i.Scan)
            .WithMany(s => s.ResultItems)
            .HasForeignKey(i => i.ScanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
