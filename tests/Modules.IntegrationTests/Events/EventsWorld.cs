// ACCEPTANCE TEST — written by the consultant for phase P11. Do not modify; report problems instead.
namespace NIMI.UniPortal.Modules.IntegrationTests.Events;

/// <summary>What <see cref="EventsSetup.BuildAsync"/> creates.</summary>
internal sealed record EventsWorld(
    Guid Eng,
    Guid Cs,
    Guid Com,
    Guid Ahmed,
    HttpClient AhmedClient,
    Guid Mona,
    HttpClient MonaClient,
    Guid Hany,
    HttpClient HanyClient,
    Guid Sara,
    HttpClient SaraClient);
