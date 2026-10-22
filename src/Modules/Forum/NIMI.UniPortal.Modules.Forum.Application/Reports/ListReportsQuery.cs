using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Forum.Application.Reports;

/// <summary>Student Affairs' moderation queue (<c>?status=open|resolved</c>), oldest first.</summary>
public sealed record ListReportsQuery(string? Status, PageRequest Paging) : IQuery<PagedResult<ReportDto>>;
