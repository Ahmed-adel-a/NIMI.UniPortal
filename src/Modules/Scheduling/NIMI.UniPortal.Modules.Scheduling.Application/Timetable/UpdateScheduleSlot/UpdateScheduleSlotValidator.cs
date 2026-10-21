using FluentValidation;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Timetable.UpdateScheduleSlot;

internal sealed class UpdateScheduleSlotValidator : AbstractValidator<UpdateScheduleSlotCommand>
{
    public UpdateScheduleSlotValidator()
    {
        RuleFor(c => c.SubjectGroupId).NotEmpty();
        RuleFor(c => c.FacilityId).NotEmpty();
        RuleFor(c => c.DayOfWeek).InclusiveBetween(0, 6);
        RuleFor(c => c.EndTime).GreaterThan(c => c.StartTime);
        RuleFor(c => c.Recurrence).IsInEnum();
    }
}
