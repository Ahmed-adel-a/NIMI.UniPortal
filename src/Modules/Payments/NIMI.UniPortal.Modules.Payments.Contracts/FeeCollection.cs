namespace NIMI.UniPortal.Modules.Payments.Contracts;

/// <summary>Money collected and still outstanding over the report's fee items.</summary>
public sealed record FeeCollection(decimal Collected, decimal Outstanding);
