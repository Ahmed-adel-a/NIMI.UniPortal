using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Platform.Application.AcademicSystems;

/// <summary>Decision LIFECYCLEDEPT: every level's academic system, for Student Affairs.</summary>
public sealed record GetAcademicSystemsOverviewQuery : IQuery<AcademicSystemsOverviewDto>;
