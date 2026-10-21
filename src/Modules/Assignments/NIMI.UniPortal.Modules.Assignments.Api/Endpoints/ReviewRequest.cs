namespace NIMI.UniPortal.Modules.Assignments.Api.Endpoints;

/// <summary>PUT /api/submissions/{id}/review.</summary>
public sealed record ReviewRequest(string? Decision, decimal? Grade, string? Feedback, string? RejectionReason);
