using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.UnitTests.SharedKernel.TestDoubles;

/// <summary>Returns the text; records "handler" in the trace list when a test registered one.</summary>
public sealed class EchoHandler(IServiceProvider services) : IRequestHandler<Echo, string>
{
    public Task<string> Handle(Echo request, CancellationToken cancellationToken)
    {
        services.GetService<List<string>>()?.Add("handler");
        return Task.FromResult(request.Text);
    }
}
