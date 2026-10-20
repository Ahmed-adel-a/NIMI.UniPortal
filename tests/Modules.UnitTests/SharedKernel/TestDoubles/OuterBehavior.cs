using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.UnitTests.SharedKernel.TestDoubles;

/// <summary>Records when it runs around the rest of the pipeline (registered first → outermost).</summary>
public sealed class OuterBehavior(List<string> trace) : IPipelineBehavior<Echo, string>
{
    public async Task<string> Handle(Echo request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        trace.Add("outer:before");
        var result = await next();
        trace.Add("outer:after");
        return result;
    }
}
