namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;

/// <summary>
/// Decision D2: self-registration is auto-approved, so the system must enforce every rule. The order and the exact
/// messages are part of the API contract (phase §6a): offerings show all broken rules as <c>reasons</c>, registering
/// reports the first one. The seat check is deliberately not here — it is the atomic <c>group_seats</c> counter.
/// </summary>
public static class RegistrationPolicy
{
    public static IReadOnlyList<string> Check(RegistrationContext context)
    {
        var reasons = new List<string>();

        // 1. R1 — a window exists for the term and the clock is inside it.
        if (context.Window is null || !context.Window.IsOpen(context.Now))
            reasons.Add("Registration is closed.");

        // 2. R2 — the group is active and belongs to the registration term.
        if (!context.GroupIsOffered)
            reasons.Add("This group is not offered.");

        // 3. R4 — one registration per subject per term (dropping frees it again).
        if (context.AlreadyRegisteredForSubject)
            reasons.Add("Already registered for this subject.");

        // 4. R9 — military education has its own flow (P5.5).
        if (context.IsMilitaryEducation)
            reasons.Add("Registered through the military-education process.");

        // 5. R3 — a department-restricted group is only for that department.
        if (context.TargetDepartmentId is { } target && target != context.StudentDepartmentId)
            reasons.Add("This group is reserved for another department.");

        // 6. R5 — every prerequisite must already be passed (credit_ledger.passed).
        if (context.MissingPrerequisiteCodes.Count > 0)
            reasons.Add($"Missing prerequisite: {string.Join(", ", context.MissingPrerequisiteCodes)}.");

        // 7. R6 — the window's maximum (skipped without a window; rule 1 already reported that).
        if (context.Window is { } window && context.CurrentCredits + context.SubjectCredits > window.MaxCredits)
            reasons.Add($"Exceeds the maximum of {window.MaxCredits} credit hours.");

        // 8. R7 — no overlap with the student's other registered groups.
        if (context.ClashingGroups.Count > 0)
            reasons.Add($"Time clash with {string.Join(", ", context.ClashingGroups)}.");

        return reasons;
    }

    /// <summary>Throws the first broken rule as a 400 on <c>subjectGroupId</c> (phase §6a).</summary>
    public static void EnsureCanRegister(RegistrationContext context)
    {
        var reasons = Check(context);
        if (reasons.Count > 0)
            throw new ArgumentException(reasons[0], "subjectGroupId");
    }
}
