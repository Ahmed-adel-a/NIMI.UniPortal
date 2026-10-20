using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Users.Application.Organization.UpdateFaculty;

/// <summary>Full replace; <see cref="IsActive"/> = false deactivates (nothing is ever deleted).</summary>
public sealed record UpdateFacultyCommand(Guid Id, string Code, string Name, string NameAr, bool IsActive) : ICommand<FacultyDto>;
