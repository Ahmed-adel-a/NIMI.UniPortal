using FluentValidation;

namespace NIMI.UniPortal.Modules.UnitTests.SharedKernel.TestDoubles;

/// <summary>A rename needs a name.</summary>
public sealed class RenameValidator : AbstractValidator<Rename>
{
    public RenameValidator() => RuleFor(r => r.Name).NotEmpty();
}
