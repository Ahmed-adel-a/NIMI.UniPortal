using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Attendance.Application.Abstractions;
using NIMI.UniPortal.Modules.Attendance.Application.Common;
using NIMI.UniPortal.Modules.Attendance.Domain.Sessions;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Attendance.Application.Sessions.GetSessionQr;

internal sealed class GetSessionQrHandler(IAttendanceDbContext db, ISchedulingApi scheduling, TimeProvider clock)
    : IRequestHandler<GetSessionQrQuery, SessionQrDto>
{
    public async Task<SessionQrDto> Handle(GetSessionQrQuery query, CancellationToken cancellationToken)
    {
        var session = await db.AttendanceSessions.AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == query.SessionId, cancellationToken)
            ?? throw new NotFoundException("Attendance session not found.");

        await AttendanceAccess.EnsureTeachingAsync(scheduling, session, query.CallerId, cancellationToken);

        if (!session.IsOpen)
            throw new ConflictException("The session is closed.");

        var now = clock.GetUtcNow();
        return new SessionQrDto(
            QrToken.Issue(session.Id, session.QrSecret, now),
            now.AddSeconds(QrToken.LifetimeSeconds));
    }
}
