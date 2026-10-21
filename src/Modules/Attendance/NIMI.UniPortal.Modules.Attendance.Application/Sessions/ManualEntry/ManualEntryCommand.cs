using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Attendance.Application.Sessions.ManualEntry;

/// <summary>P6.3: an audited manual correction for one student of the session's group (rule R7).</summary>
public sealed record ManualEntryCommand(
    Guid SessionId,
    Guid StudentId,
    string Status,
    string Note,
    Guid RecordedBy) : ICommand<ManualEntryDto>;
