using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Library.Application.Loans;

/// <summary>A member renews their own loan (not overdue, within the limit, nobody waiting for the book).</summary>
public sealed record RenewCommand(Guid LoanId, Viewer Caller) : ICommand<LoanDto>;
