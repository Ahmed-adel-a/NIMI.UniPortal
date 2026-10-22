using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Library.Application.Reminders;

/// <summary>
/// LIBREMIND — expire uncollected holds (the copy passes on), then remind due-soon (2 days) and overdue loans, at most
/// once per loan, kind and day. Runs daily in the background and on demand for Student Affairs.
/// </summary>
public sealed record RunRemindersCommand : ICommand<ReminderRunDto>;
