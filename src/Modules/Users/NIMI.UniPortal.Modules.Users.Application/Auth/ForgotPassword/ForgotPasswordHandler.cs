using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Notifications.Contracts;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.Modules.Users.Domain.Accounts;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Users.Application.Auth.ForgotPassword;

internal sealed class ForgotPasswordHandler(
    IUsersDbContext db, IEventCollector events, IOptions<AccountLinks> links, TimeProvider clock)
    : IRequestHandler<ForgotPasswordCommand, Unit>
{
    public async Task<Unit> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var email = command.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email && u.IsActive, cancellationToken);
        if (user is null)
            return Unit.Value;

        var now = clock.GetUtcNow();
        var recent = await db.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.CreatedAt > now.AddHours(-1))
            .ToListAsync(cancellationToken);
        if (recent.Count >= PasswordResetToken.MaxRequestsPerHour)
            return Unit.Value;

        // Only the newest link works.
        var open = await db.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.EndedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in open)
            token.End(now);

        var (value, hash) = OpaqueToken.New();
        db.PasswordResetTokens.Add(PasswordResetToken.Issue(user.Id, hash, now));

        var link = links.Value.ResetPassword(value);
        var minutes = (int)PasswordResetToken.Lifetime.TotalMinutes;
        events.Add(new EmailRequested(
            Guid.CreateVersion7(),
            now,
            user.Email,
            "Reset your password | إعادة تعيين كلمة المرور",
            $"""
            Hello {user.FullName},

            Use this link to choose a new password. It works once and expires in {minutes} minutes:
            {link}

            If you did not ask for this, ignore this email; your password stays the same.

            مرحبًا {user.FullName}،

            استخدم هذا الرابط لاختيار كلمة مرور جديدة. يعمل الرابط مرة واحدة وتنتهي صلاحيته خلال {minutes} دقيقة:
            {link}

            إذا لم تطلب ذلك فتجاهل هذه الرسالة، وستبقى كلمة المرور كما هي.
            """));
        return Unit.Value;
    }
}
