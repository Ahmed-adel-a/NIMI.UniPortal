using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NIMI.UniPortal.Modules.Users.Infrastructure.Persistence.Configurations;

/// <summary>Stores enums as readable lower-case text (<c>student</c>, <c>demonstrator</c>) — matches the CHECK constraints.</summary>
internal sealed class LowerCaseEnumConverter<TEnum>() : ValueConverter<TEnum, string>(
    value => value.ToString().ToLowerInvariant(),
    text => Enum.Parse<TEnum>(text, true))
    where TEnum : struct, Enum;
