using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Platform.Application.Settings.GetAdminSettings;

/// <summary>Every setting, for the Student Affairs settings screen. Includes the version stamp needed to save.</summary>
public sealed record GetAdminSettingsQuery : IQuery<AdminSettingsDto>;
