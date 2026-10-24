namespace NIMI.UniPortal.Modules.IntegrationTests.Scheduling;

/// <summary>The scenario <see cref="TermCopyTests"/> builds: the 2026 first term with two subjects, two professors and three slots.</summary>
internal sealed record CopyWorld(
    Guid Faculty,
    Guid Cs,
    Guid Source,
    Guid Cs301,
    Guid Cs302Group1,
    Guid HallA,
    Guid HallB,
    Guid Hany,
    string HanyToken,
    Guid Mona);
