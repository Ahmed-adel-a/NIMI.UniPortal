namespace NIMI.UniPortal.SharedKernel;

/// <summary>
/// The request is valid but ambiguous: the caller must pick one of <see cref="Options"/> for <see cref="Field"/> and
/// send it again (e.g. the role, when one national ID has several accounts). The host answers 409
/// <c>choice_required</c> with one detail per option.
/// </summary>
public sealed class ChoiceRequiredException(string field, IReadOnlyList<string> options, string message) : Exception(message)
{
    public string Field { get; } = field;

    public IReadOnlyList<string> Options { get; } = options;
}
