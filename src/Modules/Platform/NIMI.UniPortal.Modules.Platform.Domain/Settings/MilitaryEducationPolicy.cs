namespace NIMI.UniPortal.Modules.Platform.Domain.Settings;

/// <summary>
/// Decision D5: military education is configurable, never hardcoded. Only the mode exists today;
/// timing/criteria (e.g. gender, year) are designed with the military-education flow in P5.5.
/// </summary>
public sealed record MilitaryEducationPolicy(MilitaryEducationMode Mode);
