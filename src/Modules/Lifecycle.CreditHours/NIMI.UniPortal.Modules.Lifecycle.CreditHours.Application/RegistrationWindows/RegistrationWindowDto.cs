using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.RegistrationWindows;

/// <summary>A term's registration window, with <see cref="IsOpen"/> computed against the caller's clock (P4.2).</summary>
public sealed record RegistrationWindowDto(
    Guid Id,
    Guid TermId,
    DateTimeOffset OpensAt,
    DateTimeOffset ClosesAt,
    int MinCredits,
    int MaxCredits,
    bool IsOpen)
{
    public static RegistrationWindowDto From(RegistrationWindow window, DateTimeOffset now) =>
        new(
            window.Id,
            window.TermId,
            window.OpensAt,
            window.ClosesAt,
            window.MinCredits,
            window.MaxCredits,
            window.IsOpen(now));
}
