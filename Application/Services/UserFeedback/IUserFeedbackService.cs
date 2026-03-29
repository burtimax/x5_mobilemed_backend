using Infrastructure.Db.App.Entities;

namespace Application.Services.UserFeedback;

public interface IUserFeedbackService
{
    Task<UserFeedbackEntity> SaveAsync(Guid userId, string feedbackJson, CancellationToken ct = default);
}
