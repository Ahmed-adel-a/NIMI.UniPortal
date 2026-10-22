using FluentValidation;

namespace NIMI.UniPortal.Modules.Platform.Application.AcademicSystems;

internal sealed class SetAcademicSystemValidator : AbstractValidator<SetAcademicSystemCommand>
{
    public SetAcademicSystemValidator()
    {
        RuleFor(c => c.AcademicSystem)
            .Must(name => name is null || AcademicSystemNames.TryParse(name, out _))
            .WithMessage("Use credit_hours or year_groups (or null to inherit).");
        RuleFor(c => c)
            .Must(c => c.FacultyId is null || c.DepartmentId is null)
            .WithName("departmentId")
            .WithMessage("Set a faculty or a department, not both.");
    }
}
