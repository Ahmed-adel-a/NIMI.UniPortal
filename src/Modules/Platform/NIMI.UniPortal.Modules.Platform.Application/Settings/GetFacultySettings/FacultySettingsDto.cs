namespace NIMI.UniPortal.Modules.Platform.Application.Settings.GetFacultySettings;

/// <summary>
/// One faculty's overrides, the effective rules they produce and the version stamp needed to save (P1.4).
/// <see cref="Version"/> is null while the faculty has no overrides.
/// </summary>
public sealed record FacultySettingsDto(
    Guid FacultyId,
    FacultySettingsOverridesDto Overrides,
    EffectiveSettingsDto Effective,
    uint? Version,
    DateTimeOffset? UpdatedAt);
