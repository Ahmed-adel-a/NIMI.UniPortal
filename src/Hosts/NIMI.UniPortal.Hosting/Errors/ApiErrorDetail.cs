namespace NIMI.UniPortal.Hosting.Errors;

/// <summary>One problem with one field of the request (JSON field name, camelCase).</summary>
public sealed record ApiErrorDetail(string Field, string Error);
