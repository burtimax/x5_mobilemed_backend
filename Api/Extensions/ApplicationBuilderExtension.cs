using Application.QuartzJobs;
using Microsoft.AspNetCore.Builder;
using Quartz;

namespace Api.Extensions;

public static class ApplicationBuilderExtension
{
    /// <summary>
    /// Регистрирует Quartz-задачи, в том числе воркер пайплайна для обработки очереди заданий.
    /// </summary>
    public static IApplicationBuilder ScheduleQuartzJobs(this IApplicationBuilder builder, IScheduler scheduler)
    {
        // Отправка ошибок в Telegram: раз в 15 секунд забирает из очереди и шлёт в группу
        IJobDetail telegramErrorSenderJob = JobBuilder.Create<TelegramErrorSenderJob>()
            .WithIdentity("telegram_error_sender", "errors")
            .Build();
        ITrigger telegramErrorSenderTrigger = TriggerBuilder.Create()
            .WithIdentity("telegram_error_sender_trigger", "errors")
            .WithSimpleSchedule(x => x.WithIntervalInSeconds(15).RepeatForever())
            .Build();
        scheduler.ScheduleJob(telegramErrorSenderJob, telegramErrorSenderTrigger);

        IJobDetail generateWeekRationJob = JobBuilder.Create<GenerateWeekRationJob>()
            .WithIdentity("generate_week_ration", "rations")
            .Build();
        ITrigger generateWeekRationTrigger = TriggerBuilder.Create()
            .WithIdentity("generate_week_ration_trigger", "rations")
            .WithSimpleSchedule(x => x.WithIntervalInSeconds(2).RepeatForever())
            .Build();
        scheduler.ScheduleJob(generateWeekRationJob, generateWeekRationTrigger);

        return builder;
    }
}
