using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Terms.ActivateTerm;

public sealed record ActivateTermCommand(Guid Id) : ICommand<TermDto>;
