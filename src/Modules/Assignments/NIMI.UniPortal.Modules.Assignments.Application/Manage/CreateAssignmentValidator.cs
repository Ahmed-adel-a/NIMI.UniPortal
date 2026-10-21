using FluentValidation;

namespace NIMI.UniPortal.Modules.Assignments.Application.Manage;

/// <summary>Shape checks for creating an assignment; the domain enforces the dates, grades and file rules.</summary>
internal sealed class CreateAssignmentValidator : AbstractValidator<CreateAssignmentCommand>
{
    public CreateAssignmentValidator()
    {
        RuleFor(c => c.SubjectId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(255);
        RuleFor(c => c.Instructions).MaximumLength(10000);
    }
}
