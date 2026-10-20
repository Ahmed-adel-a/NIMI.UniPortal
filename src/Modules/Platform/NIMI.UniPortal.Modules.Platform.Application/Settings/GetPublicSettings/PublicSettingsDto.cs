namespace NIMI.UniPortal.Modules.Platform.Application.Settings.GetPublicSettings;

/// <summary>Branding + calendar basics — safe for anonymous callers.</summary>
public sealed record PublicSettingsDto(
    string UniversityName,
    string UniversityNameAr,
    string? LogoUrl,
    string PrimaryColor,
    string SecondaryColor,
    string TimeZone,
    int WeekStartDay,
    int[] WorkingDays);
