using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Policies;

/// <summary>Removes a faculty's override, so the university-wide policy applies to it again.</summary>
public sealed record DeleteFacultyPromotionPolicyCommand(Guid FacultyId) : ICommand<Unit>;
