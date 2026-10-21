namespace NIMI.UniPortal.Modules.Materials.Application.Browse;

/// <summary>An open stored file plus the name and type to send it back with; the endpoint disposes the stream.</summary>
public sealed record MaterialFileDto(Stream Content, string FileName, string ContentType);
