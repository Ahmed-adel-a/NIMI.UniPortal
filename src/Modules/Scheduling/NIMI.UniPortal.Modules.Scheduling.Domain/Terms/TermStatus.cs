namespace NIMI.UniPortal.Modules.Scheduling.Domain.Terms;

/// <summary>Planned → active → closed (rule R1/R2). Only one term may be active at a time.</summary>
public enum TermStatus
{
    Planned,
    Active,
    Closed,
}
