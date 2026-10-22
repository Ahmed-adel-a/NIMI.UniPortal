using NIMI.UniPortal.Modules.Events.Domain;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Events.Application.Common;

/// <summary>Parses the optional <c>?status=</c> filter.</summary>
internal static class EventStatuses
{
    public static EventStatus? Parse(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;
        return Enum.TryParse<EventStatus>(status, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw ValidationErrors.For("status", "Status must be draft, published or cancelled.");
    }
}
