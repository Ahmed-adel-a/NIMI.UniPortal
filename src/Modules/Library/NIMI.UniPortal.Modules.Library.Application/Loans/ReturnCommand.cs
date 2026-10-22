using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Library.Application.Loans;

/// <summary>Student Affairs takes a copy back; it goes to the first waiting member or to the shelf.</summary>
public sealed record ReturnCommand(Guid LoanId) : ICommand<LoanDto>;
