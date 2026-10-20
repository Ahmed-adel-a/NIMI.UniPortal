namespace NIMI.UniPortal.SharedKernel;

/// <summary>The data changed since the caller read it (or a uniqueness rule was hit). The host answers 409.</summary>
public sealed class ConflictException(string message) : Exception(message);
