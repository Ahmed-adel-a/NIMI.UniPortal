using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NIMI.UniPortal.Modules.Library.Application.Reminders;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Library.Infrastructure.Reminders;

/// <summary>
/// LIBREMIND — runs the reminder routine once a day (the same command Student Affairs can run on demand). Off when
/// <c>Library:Reminders:Enabled</c> is false. A failed run is logged and retried the next day.
/// </summary>
internal sealed class LibraryReminderService(
    IServiceScopeFactory scopes, IConfiguration configuration, TimeProvider clock, ILogger<LibraryReminderService> logger)
    : BackgroundService
{
    public const string EnabledKey = "Library:Reminders:Enabled";
    public static readonly TimeSpan Interval = TimeSpan.FromDays(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.Equals(configuration[EnabledKey], "false", StringComparison.OrdinalIgnoreCase))
            return;

        using var timer = new PeriodicTimer(Interval, clock);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var run = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new RunRemindersCommand(), stoppingToken);
                logger.LogInformation(
                    "Library reminders: {DueSoon} due soon, {Overdue} overdue, {ExpiredHolds} holds expired.",
                    run.DueSoon, run.Overdue, run.ExpiredHolds);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "The daily library reminder run failed; it will run again tomorrow.");
            }
        }
    }
}
