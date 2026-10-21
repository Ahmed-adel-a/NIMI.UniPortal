using FluentValidation;

namespace NIMI.UniPortal.Modules.Audit.Application.Activity;

internal sealed class ListActivityValidator : AbstractValidator<ListActivityQuery>
{
    public ListActivityValidator()
    {
        RuleFor(q => q.To)
            .GreaterThanOrEqualTo(q => q.From)
            .When(q => q.From is not null && q.To is not null)
            .WithMessage("'to' must not be before 'from'.");
    }
}
