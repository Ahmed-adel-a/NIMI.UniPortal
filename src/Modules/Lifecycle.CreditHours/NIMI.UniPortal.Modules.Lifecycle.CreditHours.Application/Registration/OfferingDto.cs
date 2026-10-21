namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Registration;

/// <summary>
/// One group the student may register this term, with seats and the rules it breaks (P4.3). <see cref="Eligible"/>
/// means <see cref="Reasons"/> is empty; the register command reports only the first reason.
/// </summary>
public sealed record OfferingDto(
    Guid GroupId,
    int GroupNumber,
    Guid SubjectId,
    string SubjectCode,
    string SubjectName,
    string SubjectNameAr,
    int CreditHours,
    int Capacity,
    int SeatsLeft,
    IReadOnlyList<OfferingSlotDto> Slots,
    bool Eligible,
    IReadOnlyList<string> Reasons);
