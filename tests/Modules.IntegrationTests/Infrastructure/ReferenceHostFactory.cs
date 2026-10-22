using Host.AinShamsUniversity;

namespace NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

/// <summary>The reference host (core + CreditHours + Library) — where the Library premium module is tested.</summary>
public sealed class ReferenceHostFactory : PortalHostFactory<AinShamsHost>;
