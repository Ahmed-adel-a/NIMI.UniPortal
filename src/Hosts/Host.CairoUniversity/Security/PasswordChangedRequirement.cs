using Microsoft.AspNetCore.Authorization;

namespace Host.CairoUniversity.Security;

/// <summary>The caller has replaced their temporary password (decision AUTH). Part of every protected policy but one.</summary>
internal sealed class PasswordChangedRequirement : IAuthorizationRequirement;
