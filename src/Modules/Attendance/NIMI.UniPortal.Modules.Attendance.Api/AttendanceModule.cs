using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Attendance.Api.Endpoints;
using NIMI.UniPortal.Modules.Attendance.Application;
using NIMI.UniPortal.Modules.Attendance.Infrastructure;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Attendance.Api;

public static class AttendanceModule
{
    public static IServiceCollection AddAttendanceModule(this IServiceCollection services)
    {
        services.AddAttendanceApplication();
        services.AddAttendanceInfrastructure(provider =>
            provider.GetRequiredService<IConfiguration>().GetConnectionString(PortalDatabase.ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{PortalDatabase.ConnectionStringName}' is not configured."));
        return services;
    }

    public static IEndpointRouteBuilder MapAttendanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        AttendanceSessionEndpoints.Map(endpoints);
        AttendanceReportEndpoints.Map(endpoints);
        DeviceChangeEndpoints.Map(endpoints);
        return endpoints;
    }
}
