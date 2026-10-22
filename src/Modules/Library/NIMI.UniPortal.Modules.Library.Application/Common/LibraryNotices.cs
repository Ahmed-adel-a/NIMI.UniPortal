using System.Globalization;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Library.Domain;
using NIMI.UniPortal.Modules.Notifications.Contracts;

namespace NIMI.UniPortal.Modules.Library.Application.Common;

/// <summary>Library notifications (type <c>system</c>): a hold is ready, a loan is due soon, a loan is overdue.</summary>
internal sealed class LibraryNotices(IEventCollector events)
{
    public void Ready(Reservation reservation, string title, DateTimeOffset now) =>
        Notify(
            reservation.UserId,
            now,
            $"Ready to collect: {title}",
            $"جاهز للاستلام: {title}",
            $"Your reserved book \"{title}\" is held for you until {Day(reservation.ExpiresAt)}.",
            $"الكتاب المحجوز \"{title}\" محفوظ لك حتى {Day(reservation.ExpiresAt)}.");

    public void DueSoon(Loan loan, string title, DateTimeOffset now) =>
        Notify(
            loan.UserId,
            now,
            $"Due soon: {title}",
            $"موعد الإرجاع قريب: {title}",
            $"Please return or renew \"{title}\" by {Day(loan.DueAt)}.",
            $"يرجى إرجاع أو تجديد \"{title}\" قبل {Day(loan.DueAt)}.");

    public void Overdue(Loan loan, string title, DateTimeOffset now) =>
        Notify(
            loan.UserId,
            now,
            $"Overdue: {title}",
            $"متأخر: {title}",
            $"\"{title}\" was due on {Day(loan.DueAt)}. Please return it.",
            $"كان موعد إرجاع \"{title}\" في {Day(loan.DueAt)}. يرجى إرجاعه.");

    private void Notify(Guid userId, DateTimeOffset now, string title, string titleAr, string message, string messageAr) =>
        events.Add(new NotificationRequested(
            Guid.CreateVersion7(), now, [userId], "system", Clip(title, 200), Clip(titleAr, 200), Clip(message, 2000), Clip(messageAr, 2000),
            "/library/mine"));

    private static string Day(DateTimeOffset? at) => at?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;

    private static string Clip(string text, int max) => text.Length <= max ? text : text[..(max - 3)] + "...";
}
