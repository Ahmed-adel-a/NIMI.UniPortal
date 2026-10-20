namespace NIMI.UniPortal.SharedKernel.Paging;

/// <summary><c>?page=1&amp;pageSize=20</c>, clamped: page ≥ 1, page size 1–100 (default 20).</summary>
public readonly record struct PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public PageRequest(int? page, int? pageSize)
    {
        Page = Math.Max(1, page ?? 1);
        PageSize = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
    }

    public int Page { get; }

    public int PageSize { get; }

    public int Skip => (Page - 1) * PageSize;
}
