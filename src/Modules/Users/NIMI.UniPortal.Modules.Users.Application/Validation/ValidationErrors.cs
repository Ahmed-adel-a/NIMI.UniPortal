using FluentValidation;
using FluentValidation.Results;

namespace NIMI.UniPortal.Modules.Users.Application.Validation;

/// <summary>
/// For rules that need the database (e.g. "this faculty exists"): thrown from a handler, they reach the
/// caller exactly like validator failures — 400 <c>validation_failed</c> naming the field.
/// </summary>
internal static class ValidationErrors
{
    public static ValidationException For(string field, string message) =>
        new([new ValidationFailure(field, message)]);
}
