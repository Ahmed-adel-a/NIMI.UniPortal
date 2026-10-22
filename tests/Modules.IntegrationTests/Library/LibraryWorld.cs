// ACCEPTANCE TEST — written by the consultant for phase P11. Do not modify; report problems instead.
namespace NIMI.UniPortal.Modules.IntegrationTests.Library;

/// <summary>What <see cref="LibrarySetup.BuildAsync"/> creates.</summary>
internal sealed record LibraryWorld(
    Guid Eng,
    Guid Cs,
    Guid Ahmed,
    HttpClient AhmedClient,
    Guid Mona,
    HttpClient MonaClient,
    Guid Hany,
    HttpClient HanyClient);
