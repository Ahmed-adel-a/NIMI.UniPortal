namespace NIMI.UniPortal.Modules.Library.Application.Reminders;

/// <summary>What one reminder run did.</summary>
public sealed record ReminderRunDto(int DueSoon, int Overdue, int ExpiredHolds);
