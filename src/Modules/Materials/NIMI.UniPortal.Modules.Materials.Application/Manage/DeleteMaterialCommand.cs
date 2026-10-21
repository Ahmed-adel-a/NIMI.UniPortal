using NIMI.UniPortal.Modules.Materials.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Materials.Application.Manage;

/// <summary>P7.3: soft delete by the uploader or a teacher of the subject (R4) — the stored file is kept.</summary>
public sealed record DeleteMaterialCommand(Guid MaterialId, Viewer Caller) : ICommand<Unit>;
