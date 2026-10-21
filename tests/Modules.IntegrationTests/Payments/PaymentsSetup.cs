// ACCEPTANCE TEST — written by the consultant for phase P05-payments. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.Modules.IntegrationTests.Users;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Payments;

/// <summary>Payments helpers through the real API (P5). The mock gateway's signing key is the documented default.</summary>
internal static class PaymentsSetup
{
    public const string MockSecret = "nimi-mock-secret";

    public static async Task<HttpResponseMessage> PostFeeItemAsync(
        HttpClient admin, string name, string category, decimal amount, Guid? termId = null,
        Guid? facultyId = null, Guid? departmentId = null, string dueOn = "2099-12-31", string? nameAr = null) =>
        await admin.PostAsJsonAsync("/api/admin/fee-items", new
        {
            name, nameAr = nameAr ?? name + " (ar)", description = (string?)null, category, amount, termId, facultyId, departmentId, dueOn,
        });

    public static async Task<Guid> CreateFeeItemAsync(
        HttpClient admin, string name, string category, decimal amount, Guid? termId = null,
        Guid? facultyId = null, Guid? departmentId = null, string dueOn = "2099-12-31", string? nameAr = null)
    {
        var response = await PostFeeItemAsync(admin, name, category, amount, termId, facultyId, departmentId, dueOn, nameAr);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (Guid)(await BodyOf(response))["id"]!;
    }

    public static async Task<HttpResponseMessage> RecordPaymentAsync(
        HttpClient admin, Guid studentId, Guid feeItemId, decimal amount, string method = "cash", string? reference = null) =>
        await admin.PostAsJsonAsync("/api/admin/payments", new { studentId, feeItemId, amount, method, reference });

    public static async Task<JsonObject> RecordedAsync(HttpClient admin, Guid studentId, Guid feeItemId, decimal amount, string method = "cash")
    {
        var response = await RecordPaymentAsync(admin, studentId, feeItemId, amount, method);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await BodyOf(response);
    }

    public static async Task<JsonObject> FeesAsync(HttpClient student, string query = "") =>
        (await student.GetFromJsonAsync<JsonObject>("/api/payments/fees" + query))!;

    public static JsonNode Fee(JsonObject fees, Guid feeItemId) =>
        fees["fees"]!.AsArray().Single(f => (Guid)f!["feeItemId"]! == feeItemId)!;

    public static async Task<HttpResponseMessage> CheckoutAsync(HttpClient student, Guid feeItemId, decimal amount) =>
        await student.PostAsJsonAsync("/api/payments/checkout", new { feeItemId, amount });

    public static async Task<Guid> CheckedOutAsync(HttpClient student, Guid feeItemId, decimal amount)
    {
        var response = await CheckoutAsync(student, feeItemId, amount);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (Guid)(await BodyOf(response))["paymentIntentId"]!;
    }

    public static async Task<HttpResponseMessage> CompleteMockAsync(HttpClient student, Guid intentId, string outcome) =>
        await student.PostAsJsonAsync($"/api/payments/mock/{intentId}/complete", new { outcome });

    /// <summary>Posts a mock-provider callback exactly as the provider would: raw JSON body + HMAC header.</summary>
    public static async Task<HttpResponseMessage> PostMockCallbackAsync(
        HttpClient anonymous, string providerReference, bool succeeded, decimal amount, string? signatureOverride = null)
    {
        var body = JsonSerializer.Serialize(new { providerReference, succeeded, amount, failureReason = (string?)null });
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/callbacks/mock")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("X-Mock-Signature", signatureOverride ?? Sign(body));
        return await anonymous.SendAsync(request);
    }

    public static string Sign(string body) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(MockSecret), Encoding.UTF8.GetBytes(body)));

    public static async Task<PaymentsWorld> BuildAsync(PortalWebAppFactory factory, HttpClient admin)
    {
        var eng = await OrganizationSetup.CreateFacultyAsync(admin, "ENG");
        var com = await OrganizationSetup.CreateFacultyAsync(admin, "COM");
        var cs = await OrganizationSetup.CreateDepartmentAsync(admin, eng, "CS");
        var civ = await OrganizationSetup.CreateDepartmentAsync(admin, eng, "CIV");
        var acc = await OrganizationSetup.CreateDepartmentAsync(admin, com, "ACC");
        var termId = await CreateActiveTermAsync(admin);

        var tuition = await CreateFeeItemAsync(admin, "Tuition", "tuition", 5000m, termId, nameAr: "المصروفات الدراسية");
        var labFee = await CreateFeeItemAsync(admin, "Lab fee", "other", 300m, termId, facultyId: eng, dueOn: "2020-01-01");
        var csClub = await CreateFeeItemAsync(admin, "CS club", "activity", 100m, facultyId: eng, departmentId: cs);
        var commerceFee = await CreateFeeItemAsync(admin, "Commerce fee", "other", 400m, termId, facultyId: com);

        var (ahmed, ahmedToken) = await AccountSetup.CreateStudentWithTokenAsync(factory, admin, cs, "ahmed@cu.edu.eg");
        var (mona, monaToken) = await AccountSetup.CreateStudentWithTokenAsync(factory, admin, cs, "mona@cu.edu.eg");
        var (omar, omarToken) = await AccountSetup.CreateStudentWithTokenAsync(factory, admin, civ, "omar@cu.edu.eg");
        var (sara, saraToken) = await AccountSetup.CreateStudentWithTokenAsync(factory, admin, acc, "sara@cu.edu.eg");

        return new PaymentsWorld(
            Eng: eng, Cs: cs, Civ: civ, Com: com, Acc: acc, TermId: termId,
            Tuition: tuition, LabFee: labFee, CsClub: csClub, CommerceFee: commerceFee,
            Ahmed: ahmed, AhmedClient: factory.CreateClientWithToken(ahmedToken),
            Mona: mona, MonaClient: factory.CreateClientWithToken(monaToken),
            Omar: omar, OmarClient: factory.CreateClientWithToken(omarToken),
            Sara: sara, SaraClient: factory.CreateClientWithToken(saraToken));
    }
}
