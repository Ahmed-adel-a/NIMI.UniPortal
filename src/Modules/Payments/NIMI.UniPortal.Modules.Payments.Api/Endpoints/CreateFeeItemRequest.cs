namespace NIMI.UniPortal.Modules.Payments.Api.Endpoints;

/// <summary>Body of <c>POST /api/admin/fee-items</c> (P5.1).</summary>
public sealed record CreateFeeItemRequest(
    string Name,
    string NameAr,
    string? Description,
    string Category,
    decimal Amount,
    Guid? TermId,
    Guid? FacultyId,
    Guid? DepartmentId,
    DateOnly DueOn);
