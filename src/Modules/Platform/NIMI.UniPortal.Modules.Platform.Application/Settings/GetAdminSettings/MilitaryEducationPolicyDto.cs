using NIMI.UniPortal.Modules.Platform.Domain.Settings;

namespace NIMI.UniPortal.Modules.Platform.Application.Settings.GetAdminSettings;

/// <summary>Military-education policy over HTTP (decision D5). Mode serializes as "opt_in" / "mandatory".</summary>
public sealed record MilitaryEducationPolicyDto(MilitaryEducationMode Mode);
