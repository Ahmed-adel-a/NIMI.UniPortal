using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Platform.Application.Settings.GetEffectiveSettings;

/// <summary>The rules that effectively apply for a signed-in user (P1.4); no faculty → the university defaults.</summary>
public sealed record GetEffectiveSettingsQuery(Guid? FacultyId) : IQuery<EffectiveSettingsDto>;
