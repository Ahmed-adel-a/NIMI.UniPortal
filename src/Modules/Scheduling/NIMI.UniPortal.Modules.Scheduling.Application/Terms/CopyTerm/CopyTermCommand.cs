using NIMI.UniPortal.Modules.Scheduling.Domain.Terms;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Terms.CopyTerm;

/// <summary>
/// Creates a planned term from an earlier one: the new term's own calendar, plus the source term's groups, teaching
/// assignments and weekly/biweekly timetable slots. <see cref="CreatedBy"/> is the admin doing the copy.
/// </summary>
public sealed record CopyTermCommand(
    Guid SourceTermId,
    int AcademicYear,
    TermNumber Number,
    DateOnly StartsOn,
    DateOnly EndsOn,
    int WeekCount,
    Guid CreatedBy) : ICommand<CopyTermResultDto>;
