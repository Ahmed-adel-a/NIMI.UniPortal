using FluentValidation;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Registration.RegisterForSubject;

/// <summary>Shape check only; "this group exists/offered" needs Scheduling and is checked in the handler.</summary>
internal sealed class RegisterForSubjectValidator : AbstractValidator<RegisterForSubjectCommand>
{
    public RegisterForSubjectValidator()
    {
        RuleFor(c => c.SubjectGroupId).NotEmpty();
    }
}
