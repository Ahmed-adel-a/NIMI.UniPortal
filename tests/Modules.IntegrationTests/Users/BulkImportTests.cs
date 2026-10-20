using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

namespace NIMI.UniPortal.Modules.IntegrationTests.Users;

/// <summary>Bulk CSV import of a cohort (US-A01 bulk).</summary>
public sealed class BulkImportTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    private const string Url = "/api/admin/users/bulk";
    private const string Header = "full_name,email,national_id,department_code,enrollment_year";

    [Fact]
    public async Task A_clean_file_creates_every_student_with_codes_in_file_order()
    {
        await Departments("CS");
        var csv = new StringBuilder(Header + "\n");
        for (var i = 1; i <= 10; i++)
            csv.Append($"طالب رقم {i},student{i}@cu.edu.eg,2990101000{i:00},cs,2026\n");

        var report = await Import(csv.ToString());

        Assert.Equal(10, (int)report["created"]!);
        Assert.Equal(0, (int)report["failed"]!);
        var rows = report["rows"]!.AsArray();
        Assert.Equal("CS-2026-0001", (string?)rows[0]!["studentCode"]);
        Assert.Equal("CS-2026-0010", (string?)rows[9]!["studentCode"]);
        Assert.All(rows, r => Assert.Equal("created", (string?)r!["status"]));

        // A created student can sign in with the temporary password from the report (and must change it).
        var login = await Factory.LoginAsync("student3@cu.edu.eg", (string)rows[2]!["temporaryPassword"]!);
        Assert.True((bool)login["mustChangePassword"]!);
        var me = (await Factory.CreateClientWithToken((string)login["accessToken"]!).GetFromJsonAsync<JsonObject>("/api/auth/me"))!;
        Assert.Equal("طالب رقم 3", (string?)me["fullName"]);   // Arabic names survive the CSV round trip
    }

    [Fact]
    public async Task Bad_rows_are_reported_one_by_one_and_never_block_the_good_ones()
    {
        await Departments("CS");
        var csv = $"""
            {Header}
            Good One,good1@cu.edu.eg,,CS,2026
            ,noname@cu.edu.eg,,CS,2026
            Bad Email,not-an-email,,CS,2026
            Unknown Dept,unknown@cu.edu.eg,,XYZ,2026
            Bad Year,badyear@cu.edu.eg,,CS,twenty
            Good Two,good2@cu.edu.eg,,CS,2026
            Twin,GOOD1@cu.edu.eg,,CS,2026
            """;

        var report = await Import(csv);

        Assert.Equal(2, (int)report["created"]!);
        Assert.Equal(5, (int)report["failed"]!);
        var rows = report["rows"]!.AsArray();
        Assert.Equal(new[] { "created", "error", "error", "error", "error", "created", "error" }, rows.Select(r => (string)r!["status"]!).ToArray());
        Assert.Contains("full_name", (string?)rows[1]!["error"]);
        Assert.Contains("department_code", (string?)rows[3]!["error"]);
        Assert.Contains("row 1", (string?)rows[6]!["error"]);   // duplicate inside the file points at the first one
        Assert.Equal("CS-2026-0002", (string?)rows[5]!["studentCode"]);   // no number burned on rejected rows
    }

    [Fact]
    public async Task Re_running_the_same_file_skips_everyone()
    {
        await Departments("CS");
        var csv = $"{Header}\nA,a@cu.edu.eg,,CS,2026\nB,b@cu.edu.eg,,CS,2026\n";
        await Import(csv);

        var again = await Import(csv);

        Assert.Equal(0, (int)again["created"]!);
        Assert.Equal(2, (int)again["skipped"]!);
    }

    [Fact]
    public async Task A_file_without_the_required_columns_is_a_400_on_file()
    {
        var response = await Admin().PostAsync(Url, CsvForm("name,email\nA,a@cu.edu.eg\n"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = (await response.Content.ReadFromJsonAsync<JsonObject>())!["details"]![0]!;
        Assert.Equal("file", (string?)error["field"]);
        Assert.Contains("department_code", (string?)error["error"]);
    }

    [Fact]
    public async Task A_header_only_file_is_a_400()
    {
        var response = await Admin().PostAsync(Url, CsvForm(Header + "\n"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Only_admins_import()
    {
        var response = await Factory.CreateClientAs("professor").PostAsync(Url, CsvForm(Header + "\nA,a@cu.edu.eg,,CS,2026\n"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");

    private async Task Departments(params string[] codes)
    {
        var faculty = await OrganizationSetup.CreateFacultyAsync(Admin(), "ENG");
        foreach (var code in codes)
            await OrganizationSetup.CreateDepartmentAsync(Admin(), faculty, code);
    }

    private async Task<JsonObject> Import(string csv)
    {
        var response = await Admin().PostAsync(Url, CsvForm(csv));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonObject>())!;
    }

    private static MultipartFormDataContent CsvForm(string csv)
    {
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        return new MultipartFormDataContent { { file, "file", "cohort.csv" } };
    }
}
