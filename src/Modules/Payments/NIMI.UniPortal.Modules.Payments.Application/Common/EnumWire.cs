using System.Text.Json;

namespace NIMI.UniPortal.Modules.Payments.Application.Common;

/// <summary>Serializes domain enums as the snake_case strings used by the API (JSON convention §9).</summary>
internal static class EnumWire
{
    public static string ToWire<TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString());
}
