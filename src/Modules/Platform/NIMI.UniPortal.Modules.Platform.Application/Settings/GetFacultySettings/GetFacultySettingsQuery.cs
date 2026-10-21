using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Platform.Application.Settings.GetFacultySettings;

/// <summary>The faculty's stored overrides (all null when it has none) plus the rules that effectively apply.</summary>
public sealed record GetFacultySettingsQuery(Guid FacultyId) : IQuery<FacultySettingsDto>;
