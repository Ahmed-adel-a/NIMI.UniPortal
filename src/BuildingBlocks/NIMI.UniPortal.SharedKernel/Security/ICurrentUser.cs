namespace NIMI.UniPortal.SharedKernel.Security;

/// <summary>
/// The signed-in user behind the current request, for cross-cutting records such as the activity log (P10.2) — so
/// commands need not carry an actor id just to say who did it. Null outside a signed-in request.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
}
