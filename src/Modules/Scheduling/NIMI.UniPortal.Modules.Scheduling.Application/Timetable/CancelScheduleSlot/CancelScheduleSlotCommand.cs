using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Timetable.CancelScheduleSlot;

public sealed record CancelScheduleSlotCommand(Guid Id) : ICommand<Unit>;
