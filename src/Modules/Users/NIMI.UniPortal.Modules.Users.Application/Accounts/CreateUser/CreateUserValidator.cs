using FluentValidation;
using NIMI.UniPortal.Modules.Users.Application.Auth;
using NIMI.UniPortal.Modules.Users.Domain.Accounts;

namespace NIMI.UniPortal.Modules.Users.Application.Accounts.CreateUser;

internal sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(c => c.Role).IsInEnum();
        RuleFor(c => c.FullName).NotEmpty().MaximumLength(255);
        RuleFor(c => c.Email).NotEmpty().MaximumLength(255).EmailAddress();
        RuleFor(c => c.Password!).StrongPassword().When(c => c.Password is not null);
        RuleFor(c => c.NationalId).MaximumLength(50);
        RuleFor(c => c.Phone).MaximumLength(20);
        RuleFor(c => c.Gender).IsInEnum().When(c => c.Gender is not null);

        When(c => c.Role == UserRole.Student, () =>
        {
            RuleFor(c => c.DepartmentId).NotEmpty().WithMessage("Students belong to a department.");
            RuleFor(c => c.EnrollmentYear).NotNull().InclusiveBetween(1950, 2100);
            RuleFor(c => c.AcademicTitle).Null().WithMessage("Only teaching staff have an academic title.");
        });

        When(c => c.Role == UserRole.Professor, () =>
        {
            RuleFor(c => c.DepartmentId).NotEmpty().WithMessage("Teaching staff belong to a department.");
            RuleFor(c => c.AcademicTitle).NotNull().IsInEnum();
            RuleFor(c => c.EnrollmentYear).Null().WithMessage("Only students have an enrollment year.");
        });

        When(c => c.Role == UserRole.DepartmentHead, () =>
        {
            RuleFor(c => c.DepartmentId).NotEmpty().WithMessage("A department head belongs to a department.");
            RuleFor(c => c.AcademicTitle).IsInEnum().When(c => c.AcademicTitle is not null);
            RuleFor(c => c.EnrollmentYear).Null().WithMessage("Only students have an enrollment year.");
        });

        When(c => c.Role == UserRole.Admin, () =>
        {
            RuleFor(c => c.DepartmentId).Null().WithMessage("Student Affairs accounts are university-wide (no department).");
            RuleFor(c => c.EnrollmentYear).Null();
            RuleFor(c => c.AcademicTitle).Null();
        });
    }
}
