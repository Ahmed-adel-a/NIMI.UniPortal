namespace NIMI.UniPortal.Modules.Attendance.Api.Endpoints;

/// <summary>POST /api/attendance/sessions/{id}/manual. Status and note are nullable so a missing one becomes a 400 naming it.</summary>
public sealed record ManualEntryRequest(Guid StudentId, string? Status, string? Note);
