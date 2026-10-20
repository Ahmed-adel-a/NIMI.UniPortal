using System.Text;

namespace NIMI.UniPortal.SharedKernel.Security;

/// <summary>
/// Access-token settings (section <c>Auth:Jwt</c>). One signing key per deployment, from env / user secrets —
/// never committed (trap #10). The Users module issues tokens with it; the host validates them.
/// </summary>
public sealed class PortalJwtOptions
{
    public const string SectionName = "Auth:Jwt";

    /// <summary>HS256 needs at least 256 bits.</summary>
    public const int MinimumKeyBytes = 32;

    public string Issuer { get; set; } = "nimi-uniportal";

    public string Audience { get; set; } = "nimi-uniportal";

    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Decision AUTH: 8-hour tokens, no refresh tokens in the MVP.</summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromHours(8);

    public byte[] SigningKeyBytes => Encoding.UTF8.GetBytes(SigningKey);
}
