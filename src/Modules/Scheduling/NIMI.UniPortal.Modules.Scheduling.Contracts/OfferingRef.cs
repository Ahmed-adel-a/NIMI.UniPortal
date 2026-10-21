namespace NIMI.UniPortal.Modules.Scheduling.Contracts;

/// <summary>A group offered in a term with its subject and timetable slots (P4 registration).</summary>
public sealed record OfferingRef(GroupRef Group, SubjectRef Subject, IReadOnlyList<SlotRef> Slots);
