using Infrastructure.Db.App;
using Infrastructure.Db.App.Entities;

namespace Application.Services.UserFeedback;

public class UserFeedbackService : IUserFeedbackService
{
    private readonly AppDbContext _db;

    public UserFeedbackService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserFeedbackEntity> SaveAsync(Guid userId, string feedbackJson, CancellationToken ct = default)
    {
        var entity = new UserFeedbackEntity
        {
            UserId = userId,
            Feedback = feedbackJson,
        };

        await _db.UserFeedbacks.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
        return entity;
    }
}
