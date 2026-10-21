using FluentValidation;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog.CreateTeachingAssignment;

internal sealed class CreateTeachingAssignmentValidator : AbstractValidator<CreateTeachingAssignmentCommand>
{
    public CreateTeachingAssignmentValidator()
    {
        RuleFor(c => c.StaffId).NotEmpty();
        RuleFor(c => c.SubjectId).NotEmpty();
        RuleFor(c => c.TermId).NotEmpty();
        RuleFor(c => c.Role).IsInEnum();
    }
}
