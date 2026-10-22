using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Infrastructure.Persistence.Configurations;

/// <summary>
/// Stores enums as snake_case text (<c>promoted_with_carried</c>), matching the CHECK constraints and the JSON the
/// API speaks. Copied from CreditHours on purpose: the converter is per module.
/// </summary>
internal sealed class SnakeCaseEnumConverter<TEnum>() : ValueConverter<TEnum, string>(
    value => JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString()),
    text => Enum.Parse<TEnum>(text.Replace("_", string.Empty, StringComparison.Ordinal), true))
    where TEnum : struct, Enum;
