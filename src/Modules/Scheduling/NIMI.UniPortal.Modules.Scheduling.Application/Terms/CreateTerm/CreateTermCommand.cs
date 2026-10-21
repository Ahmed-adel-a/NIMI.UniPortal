using NIMI.UniPortal.Modules.Scheduling.Domain.Terms;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Terms.CreateTerm;

public sealed record CreateTermCommand(
    int AcademicYear,
    TermNumber Number,
    DateOnly StartsOn,
    DateOnly EndsOn,
    int WeekCount) : ICommand<TermDto>;
