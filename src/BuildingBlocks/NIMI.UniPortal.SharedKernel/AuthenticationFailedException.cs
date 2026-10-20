namespace NIMI.UniPortal.SharedKernel;

/// <summary>
/// Login refused. Deliberately says nothing about why (unknown email, wrong password, inactive, locked):
/// the host answers one identical 401 <c>invalid_credentials</c>, so accounts can't be enumerated.
/// </summary>
public sealed class AuthenticationFailedException() : Exception("Invalid email or password.");
