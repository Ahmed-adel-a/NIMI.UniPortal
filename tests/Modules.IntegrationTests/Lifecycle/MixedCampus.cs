// ACCEPTANCE TEST — written by the consultant for decision LIFECYCLEDEPT. Do not modify; report problems instead.
namespace NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;

/// <summary>
/// A university running both systems: ENG → CS on credit hours (the default), COM → ACC on year groups (faculty
/// setting). ACC101 has one group shared by both; CS101 belongs to CS. Hany teaches both; Ahmed (CS) and Sara (ACC).
/// </summary>
internal sealed record MixedCampus(
    Guid Eng,
    Guid Cs,
    Guid Com,
    Guid Acc,
    Guid TermId,
    Guid Cs101,
    Guid Cs101Group,
    Guid Acc101,
    Guid Acc101Group,
    Guid Ahmed,
    HttpClient AhmedClient,
    Guid Sara,
    HttpClient SaraClient,
    Guid Hany,
    HttpClient HanyClient);
