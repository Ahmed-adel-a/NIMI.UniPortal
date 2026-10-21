namespace NIMI.UniPortal.SharedKernel;

/// <summary>
/// The caller is signed in but has no right to this action (e.g. a student or admin opening the professor-only
/// teaching schedule, rule R10). The host answers 403 <c>forbidden</c>; the message is safe to show.
/// </summary>
public sealed class ForbiddenException(string message) : Exception(message);
