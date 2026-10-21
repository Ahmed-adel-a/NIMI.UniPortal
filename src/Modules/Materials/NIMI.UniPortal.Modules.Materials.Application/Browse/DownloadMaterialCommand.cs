using NIMI.UniPortal.Modules.Materials.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Materials.Application.Browse;

/// <summary>P7.2: download a visible material; a command because it counts the download.</summary>
public sealed record DownloadMaterialCommand(Guid MaterialId, Viewer Caller) : ICommand<MaterialFileDto>;
