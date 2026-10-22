using Systems = NIMI.UniPortal.Modules.Platform.Contracts.AcademicSystems;

namespace NIMI.UniPortal.Modules.Platform.Application.AcademicSystems;

/// <summary>
/// The deployment's built-in academic system, used where Student Affairs has set none (<c>Platform:DefaultAcademicSystem</c>,
/// e.g. year groups for a university that mostly runs them).
/// </summary>
public sealed class AcademicSystemDefaults
{
    public string Default { get; set; } = Systems.CreditHours;
}
