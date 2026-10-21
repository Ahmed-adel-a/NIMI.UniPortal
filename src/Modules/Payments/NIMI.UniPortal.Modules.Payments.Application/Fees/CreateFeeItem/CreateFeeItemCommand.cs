using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Payments.Application.Fees.CreateFeeItem;

/// <summary>P5.1 — Student Affairs defines a fee item (PAYOWN).</summary>
public sealed record CreateFeeItemCommand(
    string Name,
    string NameAr,
    string? Description,
    string Category,
    decimal Amount,
    Guid? TermId,
    Guid? FacultyId,
    Guid? DepartmentId,
    DateOnly DueOn,
    Guid CreatedBy) : ICommand<FeeItemDto>;
