using FluentValidation;

namespace NIMI.UniPortal.Modules.Announcements.Application.Manage;

internal sealed class EditAnnouncementValidator : AbstractValidator<EditAnnouncementCommand>
{
    public EditAnnouncementValidator()
    {
        RuleFor(c => c.Title).NotEmpty().MaximumLength(255);
        RuleFor(c => c.Body).NotEmpty().MaximumLength(10000);
        RuleFor(c => c.Priority).IsInEnum();
    }
}
