// ACCEPTANCE TEST — written by the consultant for phase P03. Do not modify; report problems instead.
namespace NIMI.UniPortal.Modules.IntegrationTests.Scheduling;

/// <summary>The scenario <see cref="TeachingScheduleTests"/> builds: one department, two subjects, two professors.</summary>
internal sealed record TeachingWorld(
    Guid DepartmentId,
    Guid Cs301Group1,
    Guid Cs302Group1,
    Guid HallA,
    Guid HallB,
    Guid Hany,
    Guid HanyAssignment,
    Guid Cs301Group1Slot,
    HttpClient HanyClient,
    HttpClient MonaClient);
