namespace NIMI.UniPortal.Modules.Payments.Domain.Fees;

/// <summary>The kind of fee a fee item represents (P5, schema check <c>fee_items.category</c>).</summary>
public enum FeeCategory
{
    Tuition,
    MilitaryEducation,
    Activity,
    Other,
}
