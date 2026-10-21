using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NIMI.UniPortal.Modules.Payments.Infrastructure.Persistence.Configurations;

/// <summary>
/// Stores enums as snake_case text (<c>military_education</c>, <c>bank_transfer</c>) — matches the CHECK constraints
/// and the JSON the API already speaks.
/// </summary>
internal sealed class SnakeCaseEnumConverter<TEnum>() : ValueConverter<TEnum, string>(
    value => JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString()),
    text => Enum.Parse<TEnum>(text.Replace("_", string.Empty, StringComparison.Ordinal), true))
    where TEnum : struct, Enum;
