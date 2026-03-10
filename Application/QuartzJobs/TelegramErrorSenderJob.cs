using ModuleTelegramLogger;
using Quartz;

namespace Application.QuartzJobs;

/// <summary>
/// Quartz Job: вызывает ITelegramErrorSender для отправки ошибок из очереди в Telegram-группу.
/// </summary>
[DisallowConcurrentExecution]
public sealed class TelegramErrorSenderJob : IJob
{
    private readonly ITelegramErrorSender _sender;

    public TelegramErrorSenderJob(ITelegramErrorSender sender)
    {
        _sender = sender;
    }

    public Task Execute(IJobExecutionContext context)
    {
        return _sender.SendPendingErrorsAsync(context.CancellationToken);
    }
}
