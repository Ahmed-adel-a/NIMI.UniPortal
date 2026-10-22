using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Library.Application.Abstractions;
using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.Modules.Library.Domain;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Library.Application.Reminders;

internal sealed class RunRemindersHandler(ILibraryDbContext db, HoldQueue holds, LibraryNotices notices, TimeProvider clock)
    : IRequestHandler<RunRemindersCommand, ReminderRunDto>
{
    public static readonly TimeSpan DueSoonWindow = TimeSpan.FromDays(2);

    public async Task<ReminderRunDto> Handle(RunRemindersCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        var expired = await db.Reservations
            .Where(r => r.Status == ReservationStatus.Ready && r.ExpiresAt < now)
            .OrderBy(r => r.ExpiresAt)
            .ToListAsync(cancellationToken);
        foreach (var reservation in expired)
        {
            reservation.Expire(now);
            if (reservation.CopyId is { } copyId)
                await holds.PassOnAsync(await db.Copies.SingleAsync(c => c.Id == copyId, cancellationToken), now, cancellationToken);
        }

        var soon = now + DueSoonWindow;
        var open = await db.Loans.Where(l => l.ReturnedAt == null && l.DueAt <= soon).ToListAsync(cancellationToken);
        var loanIds = open.Select(l => l.Id).ToList();
        var sentToday = (await db.Reminders.AsNoTracking()
                .Where(r => loanIds.Contains(r.LoanId) && r.SentOn == today)
                .Select(r => new { r.LoanId, r.Kind })
                .ToListAsync(cancellationToken))
            .Select(r => (r.LoanId, r.Kind))
            .ToHashSet();
        var bookIds = open.Select(l => l.BookId).Distinct().ToList();
        var titles = await db.Books.AsNoTracking().Where(b => bookIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, b => b.Title, cancellationToken);

        int dueSoon = 0, overdue = 0;
        foreach (var loan in open)
        {
            var kind = loan.IsOverdue(now) ? ReminderKind.Overdue : ReminderKind.DueSoon;
            if (!sentToday.Add((loan.Id, kind)))
                continue;

            db.Reminders.Add(LoanReminder.Record(loan.Id, kind, today));
            var title = titles.GetValueOrDefault(loan.BookId) ?? string.Empty;
            if (kind == ReminderKind.Overdue)
            {
                notices.Overdue(loan, title, now);
                overdue++;
            }
            else
            {
                notices.DueSoon(loan, title, now);
                dueSoon++;
            }
        }

        return new ReminderRunDto(dueSoon, overdue, expired.Count);
    }
}
