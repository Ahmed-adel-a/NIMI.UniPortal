namespace NIMI.UniPortal.Modules.Users.Application.Abstractions;

/// <summary>A signed bearer token and when it stops working.</summary>
public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
