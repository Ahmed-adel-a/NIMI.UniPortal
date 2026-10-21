using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Platform.Application.Settings.DeleteFacultySettings;

/// <summary>Removes every override of one faculty (P1.4). Idempotent: no row is not an error.</summary>
public sealed record DeleteFacultySettingsCommand(Guid FacultyId) : ICommand<Unit>;
