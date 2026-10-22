// ACCEPTANCE TEST — written by the consultant for phase P4.B. Do not modify; report problems instead.
namespace NIMI.UniPortal.Modules.IntegrationTests.SemesterBased;

/// <summary>
/// The semester-based campus (reference host): ENG → CS with a 4-level plan. Level 1, second term: S1–S4 (S1 has
/// two groups); level 1, first term: F1; level 2, second term: T1. Hany teaches everything; Ahmed and Mona are CS
/// students. The active term is the 2026/2027 second term.
/// </summary>
internal sealed record SemesterWorld(
    Guid Faculty,
    Guid Department,
    Guid TermId,
    IReadOnlyDictionary<string, Guid> Subjects,
    IReadOnlyDictionary<string, Guid> Groups,
    Guid Ahmed,
    HttpClient AhmedClient,
    Guid Mona,
    HttpClient MonaClient,
    Guid Hany,
    HttpClient HanyClient);
