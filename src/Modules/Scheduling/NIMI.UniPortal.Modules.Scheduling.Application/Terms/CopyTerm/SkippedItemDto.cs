namespace NIMI.UniPortal.Modules.Scheduling.Application.Terms.CopyTerm;

/// <summary>One thing the copy could not carry over, named by subject (and group, when it has one).</summary>
public sealed record SkippedItemDto(
    SkippedItemKind Kind,
    string SubjectCode,
    int? GroupNumber,
    SkipReason Reason);
