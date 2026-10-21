// ACCEPTANCE TEST — written by the consultant for phase P02. Do not modify; report problems instead.
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.UnitTests.SharedKernel.TestDoubles;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.UnitTests.SharedKernel;

/// <summary>P2.0 — the in-process EventBus and publish-after-commit in the module unit of work.</summary>
public sealed class EventBusTests
{
    // ── Collector ───────────────────────────────────────────────

    [Fact]
    public async Task The_collector_hands_out_queued_events_once_in_order()
    {
        await using var provider = Build(new EventTrace());
        await using var scope = provider.CreateAsyncScope();
        var collector = scope.ServiceProvider.GetRequiredService<IEventCollector>();
        var first = PingHappened.Named("a");
        var second = PingHappened.Named("b");

        collector.Add(first);
        collector.Add(second);

        Assert.Equal(new IIntegrationEvent[] { first, second }, collector.Drain());
        Assert.Empty(collector.Drain());
    }

    [Fact]
    public async Task The_collector_is_scoped_so_requests_never_share_events()
    {
        await using var provider = Build(new EventTrace());
        await using var scopeA = provider.CreateAsyncScope();
        await using var scopeB = provider.CreateAsyncScope();

        scopeA.ServiceProvider.GetRequiredService<IEventCollector>().Add(PingHappened.Named("a"));

        Assert.Empty(scopeB.ServiceProvider.GetRequiredService<IEventCollector>().Drain());
    }

    // ── Publisher ───────────────────────────────────────────────

    [Fact]
    public async Task Publishing_reaches_every_handler_of_the_event_in_registration_order()
    {
        var trace = new EventTrace();
        await using var provider = Build(trace, typeof(FirstPingHandler), typeof(SecondPingHandler));

        await PublishAsync(provider, PingHappened.Named("x"));

        Assert.Equal(["first:x", "second:x"], trace.Entries);
    }

    [Fact]
    public async Task A_failing_handler_is_isolated_from_the_publisher_and_the_other_handlers()
    {
        var trace = new EventTrace();
        await using var provider = Build(trace, typeof(FirstPingHandler), typeof(ThrowingPingHandler), typeof(SecondPingHandler));

        var error = await Record.ExceptionAsync(() => PublishAsync(provider, PingHappened.Named("x")));

        Assert.Null(error);
        Assert.Equal(["first:x", "second:x"], trace.Entries);
    }

    [Fact]
    public async Task An_event_without_handlers_is_simply_ignored()
    {
        await using var provider = Build(new EventTrace());

        Assert.Null(await Record.ExceptionAsync(() => PublishAsync(provider, PingHappened.Named("x"))));
    }

    [Fact]
    public async Task Handler_registration_scans_an_assembly_for_every_closed_handler()
    {
        var services = BaseServices(new EventTrace());
        services.AddIntegrationEventHandlers(typeof(EventBusTests).Assembly);
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var scope = provider.CreateAsyncScope();

        var handlers = scope.ServiceProvider.GetServices<IIntegrationEventHandler<PingHappened>>().Select(h => h.GetType()).ToList();

        Assert.Equal(3, handlers.Count);
        Assert.Contains(typeof(FirstPingHandler), handlers);
        Assert.Contains(typeof(SecondPingHandler), handlers);
        Assert.Contains(typeof(ThrowingPingHandler), handlers);
    }

    // ── Publish after commit (ModuleUnitOfWorkBehavior) ─────────

    [Fact]
    public async Task Events_of_a_successful_command_are_published_only_after_the_save()
    {
        var trace = new EventTrace();
        await using var provider = Build(trace, typeof(FirstPingHandler));

        await SendAsync(provider, new QueuePing("done"));

        Assert.Equal(["handled", "saved", "first:done"], trace.Entries);
    }

    [Fact]
    public async Task A_command_that_fails_in_its_handler_publishes_nothing_and_leaves_nothing_queued()
    {
        var trace = new EventTrace();
        await using var provider = Build(trace, typeof(FirstPingHandler));
        await using var scope = provider.CreateAsyncScope();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scope.ServiceProvider.GetRequiredService<ISender>().Send(new QueuePing("lost", FailInHandler: true)));

        Assert.Empty(trace.Entries);
        Assert.Empty(scope.ServiceProvider.GetRequiredService<IEventCollector>().Drain());
    }

    [Fact]
    public async Task A_command_whose_save_fails_publishes_nothing_and_leaves_nothing_queued()
    {
        var trace = new EventTrace { FailSave = true };
        await using var provider = Build(trace, typeof(FirstPingHandler));
        await using var scope = provider.CreateAsyncScope();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scope.ServiceProvider.GetRequiredService<ISender>().Send(new QueuePing("lost")));

        Assert.Equal(["handled"], trace.Entries);
        Assert.Empty(scope.ServiceProvider.GetRequiredService<IEventCollector>().Drain());
    }

    [Fact]
    public async Task A_failing_event_handler_never_fails_the_command_that_raised_the_event()
    {
        var trace = new EventTrace();
        await using var provider = Build(trace, typeof(ThrowingPingHandler), typeof(FirstPingHandler));

        var error = await Record.ExceptionAsync(() => SendAsync(provider, new QueuePing("ok")));

        Assert.Null(error);
        Assert.Equal(["handled", "saved", "first:ok"], trace.Entries);
    }

    private static ServiceCollection BaseServices(EventTrace trace)
    {
        var services = new ServiceCollection();
        services.AddMessaging();
        services.AddEventBus();
        services.AddSingleton(trace);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddScoped<IRequestHandler<QueuePing, Unit>, QueuePingHandler>();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(EventUnitOfWork<,>));
        return services;
    }

    private static ServiceProvider Build(EventTrace trace, params Type[] pingHandlers)
    {
        var services = BaseServices(trace);
        foreach (var handler in pingHandlers)
            services.AddScoped(typeof(IIntegrationEventHandler<PingHappened>), handler);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static async Task PublishAsync(ServiceProvider provider, IIntegrationEvent integrationEvent)
    {
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IEventPublisher>().PublishAsync(integrationEvent, CancellationToken.None);
    }

    private static async Task SendAsync<T>(ServiceProvider provider, IRequest<T> request)
    {
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }
}
