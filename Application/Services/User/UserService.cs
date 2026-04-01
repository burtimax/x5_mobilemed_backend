using Application.Configs;
using Application.Extensions;
using Application.Models.Auth;
using Application.Models.User;
using Infrastructure.Db.App;
using Infrastructure.Db.App.Entities;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Const;
using Shared.Contracts;
using Shared.Extensions;
using FastEndpoints.Security;
using Application.Services.StatEvent;
using Shared.Models;
using Pagination = Shared.Models.Pagination;
using Application.Services.Email;
using Shared.Configs;
using System.Text.Encodings.Web;
using Infrastructure.Models;
using Microsoft.AspNetCore.WebUtilities;

namespace Application.Services.User
{
    /// <summary>
    /// Сервис для управления пользователями
    /// </summary>
    public class UserService : IUserService
    {
        private readonly AppDbContext _db;
        private readonly JwtAuthTokenOption _tokenOptions;
        private readonly ILogger<UserService> _logger;
        private readonly IStatEventService _statEventService;
        private readonly AppConfiguration _appConfiguration;

        /// <summary>
        /// Инициализирует новый экземпляр сервиса пользователей
        /// </summary>
        /// <param name="db">Контекст базы данных</param>
        /// <param name="mapper">Маппер объектов</param>
        /// <param name="configuration">Конфигурация приложения</param>
        /// <param name="logger">Логгер</param>
        /// <param name="statEventService">Сервис статистики</param>
        /// <param name="emailService">Сервис отправки email</param>
        /// <param name="appConfiguration">Конфигурация приложения</param>
        public UserService(
            AppDbContext db,
            IConfiguration configuration,
            ILogger<UserService> logger,
            IStatEventService statEventService,
            AppConfiguration appConfiguration)
        {
            _db = db;
            _logger = logger;
            _statEventService = statEventService;
            _appConfiguration = appConfiguration;
            _tokenOptions = new JwtAuthTokenOption();
            configuration.GetSection("Token").Bind(_tokenOptions);
        }

        /// <summary>
        /// Получает список пользователей с фильтрацией и пагинацией
        /// </summary>
        /// <param name="r">Параметры запроса (фильтры, пагинация, сортировка)</param>
        /// <param name="cancellation">Токен отмены операции</param>
        /// <returns>Постраничный список пользователей</returns>
        public async Task<PagedList<UserEntity>> GetAsync(
            GetUserRequest r,
            CancellationToken cancellation)
        {
            var query = _db.Users
                .AsNoTracking()
                .Include(u => u.Profile)
                .WhereIf(r.Ids is not null && r.Ids.Any(), x => r.Ids!.Contains(x.Id))
                .OrderByStr(r.Order);

            return await PagedList<UserEntity>.ToPagedListAsync(query, r.PageNumber, r.PageSize);
        }

        /// <summary>
        /// Получает пользователя по идентификатору
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <returns>Сущность пользователя или null, если пользователь не найден</returns>
        public async Task<UserEntity?> GetByIdAsync(Guid userId)
        {
            return await _db.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(x => x.Id.Equals(userId));
        }


        /// <summary>
        /// Обновляет данные пользователя
        /// </summary>
        /// <param name="r">Данные для обновления пользователя</param>
        /// <param name="cancellation">Токен отмены операции</param>
        /// <returns>Обновлённая сущность пользователя</returns>
        /// <exception cref="Exception">Выбрасывается, если пользователь не найден или формат телефона неверный</exception>
        public async Task<UserEntity> UpdateAsync(
            Guid userId,
            UpdateUserRequest r,
            CancellationToken cancellation)
        {
            var user = await GetByIdAsync(userId);

            if (user is null) throw new Exception($"User not found [id = {userId}].");

            var profile = user.Profile;
            if (profile is null) throw new Exception($"User profile not found [userId = {userId}].");

            // Обновляем профиль
            if (r.Age is not null) profile.Age = r.Age;
            if (r.Height is not null) profile.Height = r.Height;
            if (r.Weight is not null) profile.Weight = r.Weight;
            if (r.Gender is not null) profile.Gender = r.Gender;
            if (r.SmokeStatus is not null) profile.SmokeStatus = r.SmokeStatus;
            if (r.Goals is not null) profile.Goals = r.Goals;

            if (r.ConfirmedPolicyAndDocuments is not null)
                user.ConfirmedPolicyAndDocuments = r.ConfirmedPolicyAndDocuments;

            _db.UserProfiles.Update(profile);
            await _db.SaveChangesAsync(cancellation);

            return user;
        }

        /// <summary>
        /// Обновляет (refresh) JWT токен для уже аутентифицированного пользователя
        /// </summary>
        /// <param name="tokenData">Данные пользователя из текущего токена</param>
        /// <param name="cancellation">Токен отмены операции</param>
        /// <returns>Новый токен и актуальные данные пользователя</returns>
        public async Task<LoginResponse> RefreshTokenAsync(
            Guid userId,
            CancellationToken cancellation)
        {
            try
            {
                _logger.LogInformation("Обновление токена для пользователя с ID: {UserId}", userId);

                var user = await GetByIdAsync(userId);
                if (user is null)
                    throw new Exception($"Пользователь не найден [id = {userId}].");

                var profile = await _db.UserProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.UserId == user.Id, cancellation);

                if (profile is null)
                {
                    profile = new UserProfileEntity
                    {
                        UserId = user.Id,
                    };

                    _db.UserProfiles.Add(profile);
                    await _db.SaveChangesAsync(cancellation);
                }

                long sessionId = DateTime.UtcNow.Ticks;
                var token = await CreateAuthTokenAsync(sessionId, user, null);

                return new LoginResponse
                {
                    User = user,
                    Profile = profile,
                    Token = token
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении токена для пользователя с ID: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Авторизует пользователя по email и паролю
        /// </summary>
        /// <param name="request">Данные для авторизации</param>
        /// <param name="cancellation">Токен отмены операции</param>
        /// <returns>Результат авторизации с пользователем, профилем и токеном</returns>
        /// <exception cref="Exception">Выбрасывается при ошибках авторизации</exception>
        public async Task<LoginResponse> LoginAsync(
            Guid? userId,
            string? utm,
            CancellationToken? cancellation)
        {
            try
            {
                _logger.LogInformation("Попытка входа пользователя {RequestExternalIdName} = {RequestExternalId}", nameof(UserEntity.Id), userId);

                // Находим пользователя по email
                UserEntity? user = null;

                if (userId is not null)
                {
                    user = await GetByIdAsync(userId.Value);
                }

                if (user == null)
                {
                    UserEntity userEntity = new()
                    {
                        Profile = new UserProfileEntity()
                        {
                            UtmSource = utm,
                        }
                    };
                    _db.Users.Add(userEntity);
                    await _db.SaveChangesAsync();
                    user = userEntity;
                }

                // Создаем токен
                long sessionId = DateTime.UtcNow.Ticks;
                var token = await CreateAuthTokenAsync(sessionId, user, utm);

                _logger.LogInformation("Успешный вход пользователя с ID: {UserId}", user.Id);

                return new LoginResponse
                {
                    User = user,
                    Token = token
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при попытке входа пользователя {nameof(UserEntity.Id)} = {userId}");
                throw;
            }
        }

        /// <summary>
        /// Создает JWT токен для аутентифицированного пользователя
        /// </summary>
        /// <param name="sessionId">Идентификатор сессии</param>
        /// <param name="user">Пользователь для которого создается токен</param>
        /// <param name="profile">Профиль пользователя</param>
        /// <param name="utm">UTM метка (опционально)</param>
        /// <returns>JWT токен в виде строки</returns>
        private async Task<string> CreateAuthTokenAsync(
            long sessionId,
            UserEntity user,
            string? utm)
        {

            var claims = new List<System.Security.Claims.Claim>
            {
                new(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
                new("Id", user.Id.ToString()),
                new("SessionId", sessionId.ToString()),
                new("Utm", utm ?? ""),
            };

            var token = JwtBearer.CreateToken(o =>
            {
                o.SigningKey = _tokenOptions.SecretKey;
                o.Issuer = _tokenOptions.Issuer;
                o.Audience = _tokenOptions.Issuer;
                o.ExpireAt = DateTime.UtcNow.AddMinutes(_tokenOptions.ExpiryMinutes);
                o.User.Claims.AddRange(claims);
            });

            return token;
        }
    }
}
