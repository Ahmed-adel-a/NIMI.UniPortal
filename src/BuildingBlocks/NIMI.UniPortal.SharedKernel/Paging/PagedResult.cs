namespace NIMI.UniPortal.SharedKernel.Paging;

/// <summary>The one response shape for every list endpoint (lists are always paged — trap #7).</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
