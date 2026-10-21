using NIMI.UniPortal.Modules.Materials.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Materials.Application.Manage;

/// <summary>P7.3: the uploader or a teacher of the subject changes title, description and week (never the file).</summary>
public sealed record EditMaterialCommand(Guid MaterialId, string Title, string? Description, int? WeekNumber, Viewer Caller)
    : ICommand<MaterialDto>;
