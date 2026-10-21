using NIMI.UniPortal.Modules.Scheduling.Contracts;

namespace NIMI.UniPortal.Modules.Grades.Application.Common;

/// <summary>A subject group being graded, with its subject (Scheduling's view of both).</summary>
public sealed record GradedGroup(GroupRef Group, SubjectRef Subject);
