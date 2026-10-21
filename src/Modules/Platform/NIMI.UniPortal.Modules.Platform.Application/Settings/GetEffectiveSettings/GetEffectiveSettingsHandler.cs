using NIMI.UniPortal.Modules.Platform.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Platform.Application.Settings.GetEffectiveSettings;

internal sealed class GetEffectiveSettingsHandler(IPlatformSettingsApi settings, IUsersApi users)
    : IRequestHandler<GetEffectiveSettingsQuery, EffectiveSettingsDto>
{
    public async Task<EffectiveSettingsDto> Handle(GetEffectiveSettingsQuery query, CancellationToken cancellationToken)
    {
        // R1: asking about a faculty that doesn't exist is a 404, not the defaults. Asked without one → defaults.
        if (query.FacultyId is { } facultyId)
            _ = await users.GetFacultyAsync(facultyId, cancellationToken)
                ?? throw new NotFoundException("Faculty not found.");

        return EffectiveSettingsDto.From(await settings.GetEffectiveSettingsAsync(query.FacultyId, cancellationToken));
    }
}
