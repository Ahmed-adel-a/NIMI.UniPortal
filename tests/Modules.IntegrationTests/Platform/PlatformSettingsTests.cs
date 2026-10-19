using System.Net;
using System.Net.Http.Json;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

namespace NIMI.UniPortal.Modules.IntegrationTests.Platform;

/// <summary>P0.1 — GET /api/platform/settings (anonymous).</summary>
[Collection(PortalCollection.Name)]
public sealed class PlatformSettingsTests(PortalWebAppFactory factory)
{
    [Fact]
    public async Task Anonymous_caller_gets_the_seeded_public_settings()
    {
        var response = await factory.CreateClient().GetAsync("/api/platform/settings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var settings = await response.Content.ReadFromJsonAsync<PublicSettings>();
        Assert.NotNull(settings);
        Assert.Equal("University", settings.UniversityName);
        Assert.Equal("الجامعة", settings.UniversityNameAr);   // Arabic survives the round trip (UTF-8 end to end)
        Assert.Null(settings.LogoUrl);
        Assert.Equal("#0A4CB3", settings.PrimaryColor);
        Assert.Equal("#5B9BFF", settings.SecondaryColor);
        Assert.Equal("Africa/Cairo", settings.TimeZone);
        Assert.Equal(0, settings.WeekStartDay);
        Assert.Equal([0, 1, 2, 3, 4, 5], settings.WorkingDays);
    }

    [Fact]
    public async Task Public_settings_do_not_expose_academic_rules()
    {
        var json = await factory.CreateClient().GetStringAsync("/api/platform/settings");

        Assert.DoesNotContain("attendanceThreshold", json);
        Assert.DoesNotContain("gradingScale", json);
        Assert.DoesNotContain("studentCodeFormat", json);
        Assert.DoesNotContain("militaryEducationPolicy", json);
    }

    private sealed record PublicSettings(
        string UniversityName,
        string UniversityNameAr,
        string? LogoUrl,
        string PrimaryColor,
        string SecondaryColor,
        string TimeZone,
        int WeekStartDay,
        int[] WorkingDays);
}
