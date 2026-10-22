using System.Text.Json;
using NIMI.UniPortal.Modules.Platform.Domain.AcademicSystems;

namespace NIMI.UniPortal.Modules.Platform.Application.AcademicSystems;

/// <summary>The API spelling of the academic systems and their sources (snake_case, like every enum in the API).</summary>
internal static class AcademicSystemNames
{
    public static string Name(AcademicSystem system) => JsonNamingPolicy.SnakeCaseLower.ConvertName(system.ToString());

    public static string Name(AcademicSystemSource source) => JsonNamingPolicy.SnakeCaseLower.ConvertName(source.ToString());

    public static bool TryParse(string? name, out AcademicSystem system)
    {
        system = default;
        return name is not null &&
               Enum.TryParse(name.Replace("_", string.Empty, StringComparison.Ordinal), ignoreCase: true, out system) &&
               Enum.IsDefined(system);
    }
}
