using System.Globalization;
using FluentValidation;
using FluentValidation.Results;
using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.Modules.Grades.Application.Common;
using NIMI.UniPortal.Modules.Grades.Application.Drafting;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Grades.Application.Importing;

internal sealed class ImportGradesHandler(GradeDrafter drafter, IAcademicLifecycle lifecycle, IUsersApi users)
    : IRequestHandler<ImportGradesCommand, GroupGradesDto>
{
    private const string ScoreRule = "Between 0 and 100, at most 2 decimals.";

    public async Task<GroupGradesDto> Handle(ImportGradesCommand command, CancellationToken cancellationToken)
    {
        if (command.Rows.Count > ImportGradesCommand.MaxRows)
            throw ValidationErrors.For("file", $"At most {ImportGradesCommand.MaxRows} rows per file.");

        var graded = await drafter.TeachingGroupAsync(command.GroupId, command.Caller, cancellationToken);

        // The roster's student codes (case-insensitive): a CSV names students by code, never by id.
        var byCode = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var studentId in await lifecycle.GetStudentsInGroupAsync(graded.Group.Id, cancellationToken))
        {
            if (await users.GetUserAsync(studentId, cancellationToken) is { StudentCode: { } code })
                byCode[code.Trim()] = studentId;
        }

        var failures = new List<ValidationFailure>();
        var entries = new List<GradeEntry>();
        var seen = new HashSet<Guid>();
        foreach (var row in command.Rows)
        {
            var codeField = $"rows[{row.Line}].studentCode";
            var code = row.StudentCode?.Trim();
            if (string.IsNullOrEmpty(code))
            {
                failures.Add(new ValidationFailure(codeField, "A student code is required."));
                continue;
            }

            if (!byCode.TryGetValue(code, out var studentId))
            {
                failures.Add(new ValidationFailure(codeField, $"'{code}' is not on the group's roster."));
                continue;
            }

            if (!seen.Add(studentId))
            {
                failures.Add(new ValidationFailure(codeField, $"'{code}' is listed more than once."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.Score))
                continue;   // not graded yet

            if (!decimal.TryParse(row.Score.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var score) ||
                score is < 0m or > 100m || decimal.Round(score, 2) != score)
            {
                failures.Add(new ValidationFailure($"rows[{row.Line}].score", ScoreRule));
                continue;
            }

            entries.Add(new GradeEntry(studentId, score));
        }

        if (failures.Count > 0)
            throw new ValidationException(failures);
        if (entries.Count == 0)
            throw ValidationErrors.For("file", "The file has no scores.");

        return await drafter.DraftAsync(graded, entries, command.Caller, cancellationToken);
    }
}
