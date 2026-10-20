using FluentValidation;

namespace NIMI.UniPortal.Modules.Users.Application.Organization;

/// <summary>Shared field rules for faculty / department requests (the domain re-checks them).</summary>
internal static class OrganizationValidationRules
{
    public static IRuleBuilderOptions<T, string> OrgCode<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().Matches("^[A-Za-z0-9]{2,10}$").WithMessage("Codes are 2–10 letters or digits, e.g. ENG or CS.");

    public static IRuleBuilderOptions<T, string> OrgName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(255);
}
