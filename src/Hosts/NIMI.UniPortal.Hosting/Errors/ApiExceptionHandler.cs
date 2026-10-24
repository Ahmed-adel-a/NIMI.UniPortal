using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.SharedKernel;
using Npgsql;

namespace NIMI.UniPortal.Hosting.Errors;

/// <summary>
/// Maps exceptions to the <see cref="ApiError"/> envelope:
/// validation → 400 · domain rule (ArgumentException thrown <b>from a .Domain assembly</b>) → 400 ·
/// malformed request → 400 · <see cref="NotFoundException"/> → 404 · <see cref="ConflictException"/> or a
/// database unique-key violation → 409 · <see cref="ChoiceRequiredException"/> → 409 <c>choice_required</c> · <see cref="TooManyRequestsException"/> → 429 · anything else → 500 without
/// internals.
/// </summary>
internal sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>PostgreSQL error code for a unique-constraint violation.</summary>
    private const string UniqueViolation = "23505";

    /// <summary>PostgreSQL error code for an exclusion-constraint violation (checked at COMMIT when deferred).</summary>
    private const string ExclusionViolation = "23P01";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, error) = exception switch
        {
            ValidationException validation => (StatusCodes.Status400BadRequest, new ApiError(
                "validation_failed",
                "One or more fields are invalid.",
                [.. validation.Errors.Select(e => new ApiErrorDetail(CamelCasePath(e.PropertyName), e.ErrorMessage))])),

            ArgumentException argument when IsDomainRule(argument) => (StatusCodes.Status400BadRequest, new ApiError(
                "invalid_value",
                "A value breaks a business rule.",
                [new ApiErrorDetail(CamelCasePath(argument.ParamName ?? ""), WithoutParameterSuffix(argument.Message))])),

            BadHttpRequestException bad => (bad.StatusCode, new ApiError(
                "bad_request",
                "The request could not be read. Check that the body is valid JSON with the expected fields.",
                [])),

            AuthenticationFailedException failed => (StatusCodes.Status401Unauthorized, new ApiError("invalid_credentials", failed.Message, [])),

            NotFoundException notFound => (StatusCodes.Status404NotFound, new ApiError("not_found", notFound.Message, [])),

            ConflictException conflict => (StatusCodes.Status409Conflict, new ApiError("conflict", conflict.Message, [])),

            ChoiceRequiredException choice => (StatusCodes.Status409Conflict, new ApiError(
                "choice_required",
                choice.Message,
                [.. choice.Options.Select(option => new ApiErrorDetail(choice.Field, option))])),

            ForbiddenException forbidden => (StatusCodes.Status403Forbidden, new ApiError("forbidden", forbidden.Message, [])),

            TooManyRequestsException limited => (StatusCodes.Status429TooManyRequests, new ApiError("rate_limited", limited.Message, [])),

            // Backstop for races the handlers' duplicate checks can't see (two admins at the same moment).
            DbUpdateException { InnerException: PostgresException { SqlState: UniqueViolation } } =>
                (StatusCodes.Status409Conflict, new ApiError("conflict", "A record with the same unique value already exists.", [])),

            // Rule R1 races: the deferrable exclusion constraint fails at COMMIT, so EF may not wrap it.
            DbUpdateException { InnerException: PostgresException { SqlState: ExclusionViolation } } =>
                (StatusCodes.Status409Conflict, new ApiError("conflict", "The change conflicts with an existing record.", [])),

            PostgresException { SqlState: ExclusionViolation } =>
                (StatusCodes.Status409Conflict, new ApiError("conflict", "The change conflicts with an existing record.", [])),

            _ => (StatusCodes.Status500InternalServerError, new ApiError("server_error", "An unexpected error occurred.", [])),
        };

        if (status >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(error, cancellationToken);
        return true;
    }

    /// <summary>Only argument errors raised by domain code are the caller's fault; elsewhere they're our bugs (→ 500).</summary>
    private static bool IsDomainRule(ArgumentException exception) =>
        exception.TargetSite?.DeclaringType?.Assembly.GetName().Name?.EndsWith(".Domain", StringComparison.Ordinal) == true;

    /// <summary>"GradingScale[0].MinScore" → "gradingScale[0].minScore" (matches the JSON field names).</summary>
    private static string CamelCasePath(string path) =>
        string.Join('.', path.Split('.').Select(part => part.Length == 0 ? part : char.ToLowerInvariant(part[0]) + part[1..]));

    private static string WithoutParameterSuffix(string message)
    {
        var cut = message.IndexOf(" (Parameter '", StringComparison.Ordinal);
        return cut < 0 ? message : message[..cut];
    }
}
