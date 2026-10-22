// ACCEPTANCE TEST — written by the consultant for phase P11. Do not modify; report problems instead.
using NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;

namespace NIMI.UniPortal.Modules.IntegrationTests.Forum;

/// <summary>What <see cref="ForumSetup.BuildForumAsync"/> creates.</summary>
internal sealed record ForumWorld(CampusWorld Campus, Guid General, Guid Engineering, Guid Sara, HttpClient SaraClient);
