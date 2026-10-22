namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Promotion;

/// <summary>What a run decided: final decisions, decisions held for fees, and students still waiting for results.</summary>
public sealed record RunPromotionsDto(int Decided, int Held, int WaitingForResults);
