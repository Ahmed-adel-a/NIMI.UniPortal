using NIMI.UniPortal.Modules.Payments.Application.Abstractions;
using NIMI.UniPortal.Modules.Payments.Application.Common;
using NIMI.UniPortal.Modules.Payments.Domain.Fees;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Payments.Application.Fees.CreateFeeItem;

internal sealed class CreateFeeItemHandler(
    IPaymentsDbContext db,
    ISchedulingApi scheduling,
    IUsersApi users,
    TimeProvider clock) : IRequestHandler<CreateFeeItemCommand, FeeItemDto>
{
    public async Task<FeeItemDto> Handle(CreateFeeItemCommand command, CancellationToken cancellationToken)
    {
        if (command.TermId is { } termId)
        {
            _ = await scheduling.GetTermAsync(termId, cancellationToken)
                ?? throw ValidationErrors.For(nameof(command.TermId), "Unknown term.");
        }

        FacultyRef? faculty = null;
        if (command.FacultyId is { } facultyId)
        {
            faculty = await users.GetFacultyAsync(facultyId, cancellationToken)
                ?? throw ValidationErrors.For(nameof(command.FacultyId), "Unknown faculty.");
        }

        DepartmentRef? department = null;
        if (command.DepartmentId is { } departmentId)
        {
            department = await users.GetDepartmentAsync(departmentId, cancellationToken)
                ?? throw ValidationErrors.For(nameof(command.DepartmentId), "Unknown department.");
        }

        if (faculty is not null && department is not null && department.FacultyId != faculty.Id)
            throw ValidationErrors.For(nameof(command.DepartmentId), "The department belongs to another faculty.");

        if (!FeeCategoryNames.TryParse(command.Category, out var category))
            throw ValidationErrors.For(nameof(command.Category), "Unknown fee category.");

        var item = FeeItem.Create(
            command.Name,
            command.NameAr,
            command.Description,
            category,
            command.Amount,
            command.TermId,
            faculty?.Id ?? department?.FacultyId,
            department?.Id,
            command.DueOn,
            command.CreatedBy,
            clock.GetUtcNow());

        db.FeeItems.Add(item);
        return FeeItemDto.From(item);
    }
}
