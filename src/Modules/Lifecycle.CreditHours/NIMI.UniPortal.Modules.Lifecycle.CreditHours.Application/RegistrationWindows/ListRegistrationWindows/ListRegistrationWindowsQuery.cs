using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.RegistrationWindows.ListRegistrationWindows;

/// <summary>P4.2 — every registration window, newest first (admin).</summary>
public sealed record ListRegistrationWindowsQuery(PageRequest Paging) : IQuery<PagedResult<RegistrationWindowDto>>;
