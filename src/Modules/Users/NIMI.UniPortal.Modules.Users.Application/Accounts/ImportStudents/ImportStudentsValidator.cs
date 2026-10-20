using FluentValidation;

namespace NIMI.UniPortal.Modules.Users.Application.Accounts.ImportStudents;

internal sealed class ImportStudentsValidator : AbstractValidator<ImportStudentsCommand>
{
    public ImportStudentsValidator() =>
        RuleFor(c => c.Rows)
            .NotEmpty().WithMessage("The file has no student rows.")
            .Must(rows => rows.Count <= ImportStudentsCommand.MaxRows)
            .WithMessage($"At most {ImportStudentsCommand.MaxRows} rows per file — split larger cohorts.")
            .OverridePropertyName("file");
}
