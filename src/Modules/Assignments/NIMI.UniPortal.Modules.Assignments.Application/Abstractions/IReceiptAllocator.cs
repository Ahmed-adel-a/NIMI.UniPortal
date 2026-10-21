namespace NIMI.UniPortal.Modules.Assignments.Application.Abstractions;

/// <summary>Hands out the next submission receipt sequence for a year — atomic, so two submissions never share one (R6).</summary>
public interface IReceiptAllocator
{
    Task<int> NextAsync(int year, CancellationToken cancellationToken);
}
