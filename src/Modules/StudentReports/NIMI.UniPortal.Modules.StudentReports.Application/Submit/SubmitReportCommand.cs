using NIMI.UniPortal.Modules.StudentReports.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.StudentReports.Application.Submit;

/// <summary>A student sends a report to the head of their department. Category: academic, attendance, conduct or other.</summary>
public sealed record SubmitReportCommand(Viewer Caller, string Category, string Title, string Message) : ICommand<StudentReportDto>;
