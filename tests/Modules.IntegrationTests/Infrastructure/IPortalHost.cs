namespace NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

/// <summary>What shared test helpers need from any booted host (Cairo or the reference host).</summary>
public interface IPortalHost
{
    TestClock Clock { get; }

    HttpClient CreateClient();

    HttpClient CreateClientAs(string role);

    HttpClient CreateClientWithToken(string accessToken);
}
