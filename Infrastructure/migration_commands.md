Команды для миграции БД.
```
# Применять команду в папке проекта
dotnet ef migrations add AddProductsWithCategories --context AppDbContext --project Infrastructure -o Db/App/Migrations
# Удаление последней миграции
dotnet ef migrations remove --context AppDbContext --project Infrastructure
# Применение миграции
dotnet ef database update --context AppDbContext --project Infrastructure
```
