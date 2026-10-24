namespace NIMI.UniPortal.Modules.Scheduling.Application.Terms.CopyTerm;

/// <summary>Why an item was left out of a copied term; the web app turns each into text in the user's language.</summary>
public enum SkipReason
{
    /// <summary>The subject was deactivated since.</summary>
    SubjectInactive,

    /// <summary>The teacher is no longer an active professor.</summary>
    StaffInactive,

    /// <summary>The hall was deactivated since.</summary>
    FacilityInactive,

    /// <summary>The day is no longer a working day for the subject's faculty.</summary>
    NotAWorkingDay,

    /// <summary>The slot's dates have no equivalent inside the new term (it is shorter).</summary>
    OutsideTerm,

    /// <summary>The slot would double-book a hall, group or teacher.</summary>
    Conflict,
}
