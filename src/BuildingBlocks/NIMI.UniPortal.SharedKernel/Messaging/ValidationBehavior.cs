using FluentValidation;

namespace NIMI.UniPortal.SharedKernel.Messaging;

/// <summary>
/// Runs every validator registered for the request before its handler. Any failure → <see cref="ValidationException"/>
/// (the host turns it into a 400 with one detail per field). Requests without validators pass straight through.
/// Registered once, outermost, by <see cref="MessagingServiceCollectionExtensions.AddMessaging"/>.
/// </summary>
internal sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var failures = new List<FluentValidation.Results.ValidationFailure>();
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);
            failures.AddRange(result.Errors);
        }

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}
