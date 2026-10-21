namespace NIMI.UniPortal.Modules.Assignments.Application.Review;

/// <summary>An open submitted file plus the name and type to send it back with; the endpoint disposes the stream.</summary>
public sealed record SubmissionFileDto(Stream Content, string FileName, string ContentType);
