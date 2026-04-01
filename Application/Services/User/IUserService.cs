using Application.Models.Auth;
using Application.Models.User;
using Infrastructure.Db.App.Entities;
using Infrastructure.Models;
using Shared.Contracts;
using Pagination = Shared.Models.Pagination;

namespace Application.Services.User
{
    public interface IUserService
    {

        /// <summary>
        /// Получение пользлователей.
        /// </summary>>
        Task<PagedList<UserEntity>> GetAsync(GetUserRequest r, CancellationToken cancellation);

        /// <summary>
        /// Обновление пользователя.
        /// </summary>
        Task<UserEntity> UpdateAsync(Guid userId, UpdateUserRequest r, CancellationToken cancellation);

        /// <summary>
        /// Получение пользователя.
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public Task<UserEntity?> GetByIdAsync(Guid userId);

        /// <summary>
        /// Авторизация пользователя по email и паролю
        /// </summary>
        /// <param name="request">Данные для авторизации</param>
        /// <param name="cancellation">Токен отмены операции</param>
        /// <returns>Результат авторизации с пользователем, профилем и токеном</returns>
        Task<LoginResponse> LoginAsync(Guid? userId, string? utm, CancellationToken? cancellation);

        /// <summary>
        /// Обновление (refresh) JWT токена по данным из текущего токена
        /// </summary>
        /// <param name="tokenData">Данные пользователя из текущего JWT токена</param>
        /// <param name="cancellation">Токен отмены операции</param>
        /// <returns>Новый токен и актуальные данные пользователя</returns>
        Task<LoginResponse> RefreshTokenAsync(Guid userId, CancellationToken cancellation);
    }
}
