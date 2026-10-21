// ACCEPTANCE TEST — written by the consultant for phase P05-payments. Do not modify; report problems instead.
namespace NIMI.UniPortal.Modules.IntegrationTests.Payments;

/// <summary>
/// What <see cref="PaymentsSetup.BuildAsync"/> creates. Organization: ENG (CS, CIV) and COM (ACC); an active term.
/// Students: Ahmed and Mona (CS), Omar (CIV), Sara (ACC). Fee items:
/// <list type="bullet">
/// <item>Tuition — whole university, this term, 5000.00, due 2099-12-31</item>
/// <item>Lab fee — Engineering, this term, 300.00, due 2020-01-01 (overdue)</item>
/// <item>CS club — Computer Science, no term (annual), 100.00, due 2099-12-31</item>
/// <item>Commerce fee — Commerce, this term, 400.00, due 2099-12-31</item>
/// </list>
/// </summary>
internal sealed record PaymentsWorld(
    Guid Eng,
    Guid Cs,
    Guid Civ,
    Guid Com,
    Guid Acc,
    Guid TermId,
    Guid Tuition,
    Guid LabFee,
    Guid CsClub,
    Guid CommerceFee,
    Guid Ahmed,
    HttpClient AhmedClient,
    Guid Mona,
    HttpClient MonaClient,
    Guid Omar,
    HttpClient OmarClient,
    Guid Sara,
    HttpClient SaraClient);
