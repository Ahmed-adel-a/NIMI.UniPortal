using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Registration.DropRegistration;

/// <summary>P4.3 — a student drops their own registered course while the window is open (rules R10).</summary>
public sealed record DropRegistrationCommand(
    Guid StudentId,
    bool IsStudent,
    Guid RegistrationId) : ICommand<Unit>;
