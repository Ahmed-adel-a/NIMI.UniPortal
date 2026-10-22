namespace NIMI.UniPortal.Modules.Library.Domain;

/// <summary>Where a physical copy is: on the shelf, lent, held for a reservation, lost or withdrawn.</summary>
public enum CopyStatus
{
    Available,
    OnLoan,
    OnHold,
    Lost,
    Withdrawn,
}
