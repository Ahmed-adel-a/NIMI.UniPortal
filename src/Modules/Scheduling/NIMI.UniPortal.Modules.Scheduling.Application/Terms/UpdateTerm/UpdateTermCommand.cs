using NIMI.UniPortal.Modules.Scheduling.Domain.Terms;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Terms.UpdateTerm;

public sealed record UpdateTermCommand(
    Guid Id,
    int AcademicYear,
    TermNumber Number,
    DateOnly StartsOn,
    DateOnly EndsOn,
    int WeekCount) : ICommand<TermDto>;
