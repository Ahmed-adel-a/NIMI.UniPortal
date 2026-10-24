namespace NIMI.UniPortal.Modules.Scheduling.Application.Terms.CopyTerm;

/// <summary>The new (planned) term, what was carried over, and what could not be — each with the reason.</summary>
public sealed record CopyTermResultDto(
    TermDto Term,
    CopiedCountsDto Copied,
    IReadOnlyList<SkippedItemDto> Skipped);
