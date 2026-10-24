using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Users.Application.Accounts.TestAccounts;

/// <summary>
/// Decision NATIONALIDLOGIN, development only: gives a seeded test account the owner's chosen password as is (the
/// password policy is not applied) and clears the forced first-login change. Never mapped to an endpoint; refused in
/// Production.
/// </summary>
public sealed record SetTestAccountPasswordCommand(Guid UserId, string Password) : ICommand<Unit>;
