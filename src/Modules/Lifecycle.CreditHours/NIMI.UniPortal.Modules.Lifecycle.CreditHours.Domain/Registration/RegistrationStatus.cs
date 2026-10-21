namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;

/// <summary>
/// The lifecycle of one student's place in a subject group (P4, decision D2). Self-registration is auto-approved,
/// so a normal row starts at <see cref="Registered"/>; <see cref="PendingPayment"/> belongs to the military-education
/// flow (P5.5) and <see cref="Completed"/>/<see cref="Failed"/> arrive with grades (P9).
/// </summary>
public enum RegistrationStatus
{
    Registered,
    PendingPayment,
    Dropped,
    Completed,
    Failed,
}
