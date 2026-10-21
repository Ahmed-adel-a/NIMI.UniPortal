using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Payments.Application.Fees.UpdateFeeItem;

/// <summary>P5.1 — edits the mutable details of a fee item; the target, category and term never change.</summary>
public sealed record UpdateFeeItemCommand(
    Guid Id,
    string Name,
    string NameAr,
    string? Description,
    decimal Amount,
    DateOnly DueOn,
    bool IsActive) : ICommand<FeeItemDto>;
