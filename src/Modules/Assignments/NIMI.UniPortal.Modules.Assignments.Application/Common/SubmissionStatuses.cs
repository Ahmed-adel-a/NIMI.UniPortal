using NIMI.UniPortal.Modules.Assignments.Domain;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Assignments.Application.Common;

/// <summary>Parses the optional <c>?status=</c> filter of the submission lists.</summary>
internal static class SubmissionStatuses
{
    public static SubmissionStatus? Parse(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;
        return Enum.TryParse<SubmissionStatus>(status, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw ValidationErrors.For("status", "Status must be submitted, approved or rejected.");
    }
}
