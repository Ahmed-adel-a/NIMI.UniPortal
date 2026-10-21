namespace NIMI.UniPortal.Modules.Payments.Domain.Fees;

/// <summary>Rule R4: derived from the paid amount, never set independently.</summary>
public enum StudentFeeStatus
{
    Pending,
    PartiallyPaid,
    Paid,
}
