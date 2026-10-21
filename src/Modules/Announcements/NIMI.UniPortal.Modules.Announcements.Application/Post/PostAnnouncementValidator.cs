using FluentValidation;

namespace NIMI.UniPortal.Modules.Announcements.Application.Post;

internal sealed class PostAnnouncementValidator : AbstractValidator<PostAnnouncementCommand>
{
    public PostAnnouncementValidator()
    {
        RuleFor(c => c.SubjectId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(255);
        RuleFor(c => c.Body).NotEmpty().MaximumLength(10000);
        RuleFor(c => c.Priority).IsInEnum();
    }
}
