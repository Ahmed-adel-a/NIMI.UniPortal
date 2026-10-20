using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Platform.Application.Settings.GetPublicSettings;

/// <summary>
/// The settings anyone may read before logging in (the login screen needs branding + the calendar basics).
/// Academic rules (thresholds, grading scale, code format, military-education policy) stay behind authentication.
/// </summary>
public sealed record GetPublicSettingsQuery : IQuery<PublicSettingsDto>;
