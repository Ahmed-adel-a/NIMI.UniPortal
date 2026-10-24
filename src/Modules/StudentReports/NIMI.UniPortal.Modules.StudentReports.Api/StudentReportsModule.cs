using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.StudentReports.Api.Endpoints;
using NIMI.UniPortal.Modules.StudentReports.Application;
using NIMI.UniPortal.Modules.StudentReports.Infrastructure;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.StudentReports.Api;

public static class StudentReportsModule
{
    public static IServiceCollection AddStudentReportsModule(this IServiceCollection services)
    {
        services.AddStudentReportsApplication();
        services.AddStudentReportsInfrastructure(provider =>
            provider.GetRequiredService<IConfiguration>().GetConnectionString(PortalDatabase.ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{PortalDatabase.ConnectionStringName}' is not configured."));
        return services;
    }

    public static IEndpointRouteBuilder MapStudentReportsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ReportEndpoints.Map(endpoints);
        DepartmentReportEndpoints.Map(endpoints);
        AdminReportEndpoints.Map(endpoints);
        return endpoints;
    }
}
