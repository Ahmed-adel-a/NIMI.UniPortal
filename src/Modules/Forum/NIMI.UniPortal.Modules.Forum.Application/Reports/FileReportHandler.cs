using NIMI.UniPortal.Modules.Forum.Application.Abstractions;
using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.Modules.Forum.Domain;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Forum.Application.Reports;

internal sealed class FileReportHandler(IForumDbContext db, ForumAccess access, VisiblePosts posts, TimeProvider clock)
    : IRequestHandler<FileReportCommand, ReportDto>
{
    public async Task<ReportDto> Handle(FileReportCommand command, CancellationToken cancellationToken)
    {
        if (command.ThreadId is { } threadId)
            _ = await access.VisibleThreadAsync(threadId, command.Caller, cancellationToken);
        if (command.PostId is { } postId)
            _ = await posts.LoadAsync(postId, command.Caller, cancellationToken);

        var report = ForumReport.File(command.ThreadId, command.PostId, command.Caller.UserId, command.Reason ?? string.Empty, clock.GetUtcNow());
        db.Reports.Add(report);
        return ReportDto.From(report);
    }
}
