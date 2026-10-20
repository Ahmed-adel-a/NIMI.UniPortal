using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Users.Application.Organization.CreateFaculty;

public sealed record CreateFacultyCommand(string Code, string Name, string NameAr) : ICommand<FacultyDto>;
