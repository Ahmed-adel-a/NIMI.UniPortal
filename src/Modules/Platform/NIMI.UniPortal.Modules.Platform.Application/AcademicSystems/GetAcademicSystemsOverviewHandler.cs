using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Platform.Application.AcademicSystems;

internal sealed class GetAcademicSystemsOverviewHandler(AcademicSystemsOverview overview)
    : IRequestHandler<GetAcademicSystemsOverviewQuery, AcademicSystemsOverviewDto>
{
    public Task<AcademicSystemsOverviewDto> Handle(GetAcademicSystemsOverviewQuery query, CancellationToken cancellationToken) =>
        overview.BuildAsync(null, cancellationToken);
}
