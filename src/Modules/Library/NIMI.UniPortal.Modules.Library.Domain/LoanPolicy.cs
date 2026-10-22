using System.Runtime.CompilerServices;

namespace NIMI.UniPortal.Modules.Library.Domain;

/// <summary>
/// The loan rules of one role (<c>student</c> or <c>professor</c>), edited by Student Affairs — no university rule is
/// hardcoded; <see cref="Default"/> gives the product's starting values until a row is saved.
/// </summary>
public sealed class LoanPolicy
{
    public const string Student = "student";
    public const string Professor = "professor";

    private LoanPolicy()
    {
    }

    public string Role { get; private set; } = string.Empty;

    public int LoanDays { get; private set; }

    public int MaxOpenLoans { get; private set; }

    public int MaxRenewals { get; private set; }

    public int HoldDays { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static IReadOnlyList<string> Roles { get; } = [Student, Professor];

    public static LoanPolicy Default(string role, DateTimeOffset now)
    {
        var policy = new LoanPolicy { Role = KnownRole(role) };
        if (policy.Role == Student)
            policy.Update(14, 3, 1, 3, now);
        else
            policy.Update(30, 10, 2, 3, now);
        return policy;
    }

    public void Update(int loanDays, int maxOpenLoans, int maxRenewals, int holdDays, DateTimeOffset now)
    {
        LoanDays = InRange(loanDays, 1, 365, nameof(loanDays));
        MaxOpenLoans = InRange(maxOpenLoans, 1, 100, nameof(maxOpenLoans));
        MaxRenewals = InRange(maxRenewals, 0, 10, nameof(maxRenewals));
        HoldDays = InRange(holdDays, 1, 30, nameof(holdDays));
        UpdatedAt = now;
    }

    // Never inlined: the host recognises a domain rule (400) by the throwing method's assembly.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int InRange(int value, int min, int max, string paramName) =>
        value >= min && value <= max ? value : throw new ArgumentOutOfRangeException(paramName, value, $"Between {min} and {max}.");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string KnownRole(string role) =>
        Roles.Contains(role) ? role : throw new ArgumentException("Loan rules exist for students and professors.", nameof(role));
}
