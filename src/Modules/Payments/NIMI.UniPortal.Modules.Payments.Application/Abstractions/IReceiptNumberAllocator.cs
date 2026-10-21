namespace NIMI.UniPortal.Modules.Payments.Application.Abstractions;

/// <summary>Atomic receipt-sequence allocation (rule R3). One sequence per receipt year.</summary>
public interface IReceiptNumberAllocator
{
    Task<int> NextAsync(int year, CancellationToken cancellationToken);
}
