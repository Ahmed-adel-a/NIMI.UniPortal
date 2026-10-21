using FluentValidation;

namespace NIMI.UniPortal.Modules.Materials.Application.Upload;

/// <summary>Text fields are checked before anything is written to disk, so a bad title never leaves an orphan file.</summary>
internal sealed class UploadMaterialValidator : AbstractValidator<UploadMaterialCommand>
{
    public UploadMaterialValidator()
    {
        RuleFor(c => c.SubjectId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(255);
        RuleFor(c => c.Description).MaximumLength(2000);
        RuleFor(c => c.WeekNumber).InclusiveBetween(1, 30).When(c => c.WeekNumber is not null);
    }
}
