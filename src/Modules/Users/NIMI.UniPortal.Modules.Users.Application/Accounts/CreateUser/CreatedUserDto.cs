namespace NIMI.UniPortal.Modules.Users.Application.Accounts.CreateUser;

/// <summary>
/// The new account. <see cref="TemporaryPassword"/> is shown <b>once</b> (only when the admin didn't choose one);
/// it is never stored or retrievable again — only its hash is.
/// </summary>
public sealed record CreatedUserDto(UserDto User, string? TemporaryPassword);
