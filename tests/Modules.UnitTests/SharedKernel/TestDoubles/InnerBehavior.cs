using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.UnitTests.SharedKernel.TestDoubles;

/// <summary>Records when it runs around the handler (registered second → inner).</summary>
public sealed class InnerBehavior(List<string> trace) : IPipelineBehavior<Echo, string>
{
    public async Task<string> Handle(Echo request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        trace.Add("inner:before");
        var result = await next();
        trace.Add("inner:after");
        return result;
    }
}
