using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using NIMI.UniPortal.SharedKernel;

namespace Host.CairoUniversity.Errors;

/// <summary>The standard error envelope for every endpoint: <c>{ code, message, details[] }</c>.</summary>
public sealed record ApiError(string Code, string Message, IReadOnlyList<ApiErrorDetail> Details);

public sealed record ApiErrorDetail(string Field, string Error);

/// <summary>
/// Maps exceptions to the envelope:
/// validation → 400 · domain rule (ArgumentException thrown <b>from a .Domain assembly</b>) → 400 ·
/// malformed request → 400 · <see cref="ConflictException"/> → 409 · anything else → 500 without internals.
/// </summary>
internal sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
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

            ConflictException conflict => (StatusCodes.Status409Conflict, new ApiError("conflict", conflict.Message, [])),

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
