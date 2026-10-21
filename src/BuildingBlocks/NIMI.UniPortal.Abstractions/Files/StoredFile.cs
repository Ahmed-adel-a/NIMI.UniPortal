namespace NIMI.UniPortal.Abstractions.Files;

/// <summary>The result of storing a file: its storage key, the number of bytes written and the content type kept.</summary>
public sealed record StoredFile(string Key, long SizeBytes, string ContentType);
