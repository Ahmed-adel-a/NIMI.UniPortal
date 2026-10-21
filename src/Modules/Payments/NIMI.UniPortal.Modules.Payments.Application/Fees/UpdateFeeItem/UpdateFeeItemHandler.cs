using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Payments.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Payments.Application.Fees.UpdateFeeItem;

internal sealed class UpdateFeeItemHandler(IPaymentsDbContext db, TimeProvider clock)
    : IRequestHandler<UpdateFeeItemCommand, FeeItemDto>
{
    public async Task<FeeItemDto> Handle(UpdateFeeItemCommand command, CancellationToken cancellationToken)
    {
        var item = await db.FeeItems.SingleOrDefaultAsync(f => f.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException("Fee item not found.");

        item.Update(
            command.Name,
            command.NameAr,
            command.Description,
            command.Amount,
            command.DueOn,
            command.IsActive,
            clock.GetUtcNow());

        return FeeItemDto.From(item);
    }
}
