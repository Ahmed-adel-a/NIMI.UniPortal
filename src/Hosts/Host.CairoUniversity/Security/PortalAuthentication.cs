using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NIMI.UniPortal.SharedKernel.Security;

namespace Host.CairoUniversity.Security;

/// <summary>
/// Validates the access tokens the Users module issues (POST /api/auth/login): our issuer + audience, signed with
/// this deployment's key (<c>Auth:Jwt:SigningKey</c> from env / user secrets — never committed).
/// </summary>
internal static class PortalAuthentication
{
    public static WebApplicationBuilder AddPortalAuthentication(this WebApplicationBuilder builder)
    {
        // Development convenience only: no key configured → a random one per run (tokens die on restart).
        // Everywhere else a missing/short key stops the host at startup instead of running insecurely.
        var keyPath = $"{PortalJwtOptions.SectionName}:SigningKey";
        if (builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(builder.Configuration[keyPath]))
            builder.Configuration[keyPath] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

        builder.Services.AddOptions<PortalJwtOptions>()
            .Bind(builder.Configuration.GetSection(PortalJwtOptions.SectionName))
            .Validate(o => o.SigningKeyBytes.Length >= PortalJwtOptions.MinimumKeyBytes,
                $"{keyPath} must be configured (at least {PortalJwtOptions.MinimumKeyBytes} bytes).")
            .ValidateOnStart();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<PortalJwtOptions>>((bearer, jwt) =>
            {
                bearer.MapInboundClaims = false;   // keep "sub" / "role" as issued
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Value.Issuer,
                    ValidAudience = jwt.Value.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(jwt.Value.SigningKeyBytes),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = PortalClaims.Name,
                    RoleClaimType = PortalClaims.Role,
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });

        // Every protected endpoint also requires the temporary password to have been replaced — except the two
        // endpoints the user needs to do exactly that (PendingPasswordChange).
        builder.Services.AddSingleton<IAuthorizationHandler, PasswordChangedHandler>();
        builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, PortalAuthorizationResultHandler>();
        builder.Services.AddAuthorizationBuilder()
            .SetDefaultPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PasswordChangedRequirement())
                .Build())
            .AddPolicy(PortalPolicies.Admin, policy => policy
                .RequireRole(PortalRoles.Admin)
                .AddRequirements(new PasswordChangedRequirement()))
            .AddPolicy(PortalPolicies.PendingPasswordChange, policy => policy.RequireAuthenticatedUser());

        return builder;
    }
}
