// ACCEPTANCE TEST — written by the consultant for phase P06. Do not modify; report problems instead.
namespace NIMI.UniPortal.Modules.IntegrationTests.Attendance;

/// <summary>
/// What <see cref="AttendanceSetup.BuildLectureAsync"/> creates: an active term around today, CS301 group 1 with a weekly
/// lecture on today's weekday that started 2 minutes ago (Cairo wall clock), taught by Hany (assignment). Ahmed, Mona and
/// Omar are registered in the group; Sara is a CS student who is not; Laila is a professor who does not teach it.
/// Grace 10 minutes and threshold 75 % (university defaults).
/// </summary>
internal sealed record LectureWorld(
    Guid FacultyId,
    Guid DepartmentId,
    Guid TermId,
    Guid SubjectId,
    Guid GroupId,
    Guid SlotId,
    DateOnly Today,
    Guid Hany,
    HttpClient HanyClient,
    HttpClient LailaClient,
    Guid Ahmed,
    HttpClient AhmedClient,
    Guid Mona,
    HttpClient MonaClient,
    Guid Omar,
    HttpClient OmarClient,
    Guid Sara,
    HttpClient SaraClient);
