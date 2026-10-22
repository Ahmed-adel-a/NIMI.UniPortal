using FluentValidation;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Standings;

internal sealed class PlaceStudentValidator : AbstractValidator<PlaceStudentCommand>
{
    public PlaceStudentValidator()
    {
        RuleFor(c => c.Level).InclusiveBetween(1, AcademicLevels.Max);
    }
}
