using NIMI.UniPortal.Modules.Payments.Application.Common;
using NIMI.UniPortal.Modules.Payments.Domain.Fees;

namespace NIMI.UniPortal.Modules.Payments.Application.Fees;

/// <summary>A fee item as Student Affairs sees it (P5.1).</summary>
public sealed record FeeItemDto(
    Guid Id,
    string Name,
    string NameAr,
    string? Description,
    string Category,
    decimal Amount,
    Guid? TermId,
    Guid? FacultyId,
    Guid? DepartmentId,
    DateOnly DueOn,
    bool IsActive)
{
    public static FeeItemDto From(FeeItem item) =>
        new(
            item.Id,
            item.Name,
            item.NameAr,
            item.Description,
            FeeCategoryNames.ToWire(item.Category),
            item.Amount,
            item.TermId,
            item.FacultyId,
            item.DepartmentId,
            item.DueOn,
            item.IsActive);
}
