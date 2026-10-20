namespace NIMI.UniPortal.Modules.Platform.Application.Abstractions;

/// <summary>
/// The settings row's version stamp: PostgreSQL's system column <c>xmin</c>, mapped as a shadow property so the
/// domain stays persistence-free. Clients echo it back on PUT; a mismatch → 409 instead of a silent overwrite.
/// </summary>
public static class SettingsVersion
{
    public const string PropertyName = "Version";
}
