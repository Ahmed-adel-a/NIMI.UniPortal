using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Terms.CloseTerm;

public sealed record CloseTermCommand(Guid Id) : ICommand<TermDto>;
