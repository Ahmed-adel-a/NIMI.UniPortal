using System.Net.Mail;

namespace NIMI.UniPortal.Modules.Users.Domain.Accounts;

/// <summary>Emails are the login name: trimmed, lower-cased, unique. Stored normalized so lookups are exact.</summary>
public static class EmailAddress
{
    public static string Normalize(string email, string paramName)
    {
        var normalized = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized.Length is 0 or > 255 || !MailAddress.TryCreate(normalized, out var parsed) || parsed.Address != normalized)
            throw new ArgumentException("Must be a valid email address.", paramName);
        return normalized;
    }
}
