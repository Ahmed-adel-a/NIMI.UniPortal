namespace NIMI.UniPortal.Modules.Attendance.Application.Sessions.ManualEntry;

/// <summary>The stored manual entry; status is <c>manual_audit</c> for present/late and <c>absent</c> otherwise.</summary>
public sealed record ManualEntryDto(Guid StudentId, string Status, string Method, string Note);
