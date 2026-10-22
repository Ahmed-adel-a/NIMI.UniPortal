namespace NIMI.UniPortal.Modules.Library.Application.Digital;

/// <summary>A digital book's file to stream to the reader; the caller disposes <see cref="Content"/>.</summary>
public sealed record DigitalBookFile(Stream Content, string ContentType, string FileName);
