using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.UnitTests.SharedKernel.TestDoubles;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.UnitTests.SharedKernel;

/// <summary>Our own mediator (decision MEDIATOR): dispatch, behavior order, one-handler rule.</summary>
public sealed class SenderTests
{
    [Fact]
    public async Task Send_runs_the_single_handler_for_the_request()
    {
        await using var provider = Build(_ => { });

        var answer = await SendAsync(provider, new Echo("hello"));

        Assert.Equal("hello", answer);
    }

    [Fact]
    public async Task Behaviors_wrap_the_handler_in_registration_order()
    {
        var trace = new List<string>();
        await using var provider = Build(services =>
        {
            services.AddSingleton(trace);
            services.AddScoped<IPipelineBehavior<Echo, string>, OuterBehavior>();
            services.AddScoped<IPipelineBehavior<Echo, string>, InnerBehavior>();
        });

        await SendAsync(provider, new Echo("x"));

        Assert.Equal(["outer:before", "inner:before", "handler", "inner:after", "outer:after"], trace);
    }

    [Fact]
    public async Task A_behavior_can_short_circuit_the_handler()
    {
        await using var provider = Build(services => services.AddScoped<IPipelineBehavior<Echo, string>, RefuseBehavior>());

        var answer = await SendAsync(provider, new Echo("ignored"));

        Assert.Equal("refused", answer);
    }

    [Fact]
    public async Task A_request_without_a_handler_fails_loudly()
    {
        await using var provider = new ServiceCollection().AddMessaging().BuildServiceProvider();

        await Assert.ThrowsAsync<InvalidOperationException>(() => SendAsync(provider, new Echo("x")));
    }

    [Fact]
    public void Registering_the_same_assembly_twice_is_rejected_one_request_one_handler()
    {
        var services = new ServiceCollection().AddRequestHandlers(typeof(SenderTests).Assembly);

        Assert.Throws<InvalidOperationException>(() => services.AddRequestHandlers(typeof(SenderTests).Assembly));
    }

    /// <summary>One scope per send — exactly how a web request uses the scoped sender.</summary>
    private static async Task<T> SendAsync<T>(ServiceProvider provider, IRequest<T> request)
    {
        await using var scope = provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    private static ServiceProvider Build(Action<IServiceCollection> configure)
    {
        // Explicit registration: scanning the whole test assembly would also pick up other test classes' handlers.
        var services = new ServiceCollection().AddMessaging().AddScoped<IRequestHandler<Echo, string>, EchoHandler>();
        configure(services);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }
}
