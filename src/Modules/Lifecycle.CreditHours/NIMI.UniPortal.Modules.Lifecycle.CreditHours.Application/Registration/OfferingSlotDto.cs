namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Registration;

/// <summary>One timetable slot of an offering, as the registration page shows it (P4.3).</summary>
public sealed record OfferingSlotDto(int DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, string FacilityName);
