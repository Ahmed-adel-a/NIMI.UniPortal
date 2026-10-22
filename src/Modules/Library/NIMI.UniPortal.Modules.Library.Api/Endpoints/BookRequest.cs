namespace NIMI.UniPortal.Modules.Library.Api.Endpoints;

/// <summary>POST /api/admin/library/books and PUT /api/admin/library/books/{id}.</summary>
public sealed record BookRequest(string? Isbn, string? Title, string? Authors, string? Publisher, int? PublicationYear, Guid? FacultyId);
