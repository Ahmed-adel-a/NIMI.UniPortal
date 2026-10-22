using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Promotion;

/// <summary>
/// P4.9: decides every student of a second term whose results are complete and who has no decision for the year,
/// and decides held students again when their fees are now paid. Publishing grades does the same automatically;
/// this is the explicit re-run for Student Affairs.
/// </summary>
public sealed record RunPromotionsCommand(Guid TermId) : ICommand<RunPromotionsDto>;
