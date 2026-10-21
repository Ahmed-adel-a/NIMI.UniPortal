using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NIMI.UniPortal.Modules.Notifications.Infrastructure.Persistence.Configurations;

/// <summary>Stores enums as readable lower-case text — matches the CHECK constraint.</summary>
internal sealed class LowerCaseEnumConverter<TEnum>() : ValueConverter<TEnum, string>(
    value => value.ToString().ToLowerInvariant(),
    text => Enum.Parse<TEnum>(text, true))
    where TEnum : struct, Enum;
