using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NIMI.UniPortal.Modules.Attendance.Infrastructure.Persistence.Configurations;

/// <summary>Stores enums as snake_case text (<c>manual_audit</c>, <c>auto_absent</c>) — matches the CHECK constraints.</summary>
internal sealed class SnakeCaseEnumConverter<TEnum>() : ValueConverter<TEnum, string>(
    value => JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString()),
    text => Enum.Parse<TEnum>(text.Replace("_", string.Empty, StringComparison.Ordinal), true))
    where TEnum : struct, Enum;
