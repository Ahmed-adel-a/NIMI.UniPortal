namespace NIMI.UniPortal.Modules.Attendance.Application.Common;

/// <summary>A confirmed member of a group as attendance screens show them (name and code come from Users).</summary>
internal sealed record MemberProfile(Guid StudentId, string FullName, string? StudentCode);
