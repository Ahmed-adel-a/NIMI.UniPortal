using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Forum.Application.Abstractions;
using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.Modules.Forum.Domain;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Forum.Application.Reports;

internal sealed class ResolveReportHandler(IForumDbContext db, TimeProvider clock) : IRequestHandler<ResolveReportCommand, ReportDto>
{
    public async Task<ReportDto> Handle(ResolveReportCommand command, CancellationToken cancellationToken)
    {
        var remove = command.Action?.Trim().ToLowerInvariant() switch
        {
            "remove" => true,
            "dismiss" => false,
            _ => throw ValidationErrors.For("action", "Action must be remove or dismiss."),
        };

        var report = await db.Reports.SingleOrDefaultAsync(r => r.Id == command.ReportId, cancellationToken)
            ?? throw new NotFoundException("Report not found.");
        if (report.Status != ForumReportStatus.Open)
            throw new ConflictException("This report has already been resolved.");

        var now = clock.GetUtcNow();
        if (remove && report.ThreadId is { } threadId &&
            await db.Threads.SingleOrDefaultAsync(t => t.Id == threadId, cancellationToken) is { } thread)
            thread.Delete(now);
        if (remove && report.PostId is { } postId &&
            await db.Posts.SingleOrDefaultAsync(p => p.Id == postId, cancellationToken) is { } post)
            post.Delete(now);

        report.Resolve(remove, command.Caller.UserId, now);
        return ReportDto.From(report);
    }
}
