namespace NIMI.UniPortal.SharedKernel;

/// <summary>The addressed record doesn't exist. The host answers 404.</summary>
public sealed class NotFoundException(string message) : Exception(message);
