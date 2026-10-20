using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.UnitTests.SharedKernel.TestDoubles;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.UnitTests.SharedKernel;

/// <summary>The two allowed pipeline steps: Validation (shared) and a module's UnitOfWork.</summary>
public sealed class PipelineBehaviorTests
{
    // ── Validation ──────────────────────────────────────────────

    [Fact]
    public async Task Invalid_request_never_reaches_the_handler()
    {
        var saves = new SaveCounter();
        await using var provider = Build(saves, validators: true);

        var error = await Assert.ThrowsAsync<ValidationException>(() => SendAsync(provider, new Rename("")));

        Assert.Contains(error.Errors, e => e.PropertyName == nameof(Rename.Name));
        Assert.Equal(0, saves.Handled);
        Assert.Equal(0, saves.Saved);
    }

    [Fact]
    public async Task Valid_request_passes_validation()
    {
        var saves = new SaveCounter();
        await using var provider = Build(saves, validators: true);

        await SendAsync(provider, new Rename("ok"));

        Assert.Equal(1, saves.Handled);
    }

    // ── UnitOfWork ──────────────────────────────────────────────

    [Fact]
    public async Task Own_command_is_saved_exactly_once_after_the_handler()
    {
        var saves = new SaveCounter();
        await using var provider = Build(saves);

        await SendAsync(provider, new Rename("x"));

        Assert.Equal(["handled", "saved"], saves.Trace);
    }

    [Fact]
    public async Task Queries_are_never_saved()
    {
        var saves = new SaveCounter();
        await using var provider = Build(saves);

        await SendAsync(provider, new Peek());

        Assert.Equal(0, saves.Saved);
    }

    [Fact]
    public async Task Another_modules_command_is_not_saved_by_this_module()
    {
        var saves = new SaveCounter();
        await using var provider = Build(saves, moduleAssembly: typeof(object).Assembly);   // "this module" lives elsewhere

        await SendAsync(provider, new Rename("x"));

        Assert.Equal(1, saves.Handled);
        Assert.Equal(0, saves.Saved);
    }

    [Fact]
    public async Task Nothing_is_saved_when_the_handler_fails()
    {
        var saves = new SaveCounter();
        await using var provider = Build(saves);

        await Assert.ThrowsAsync<InvalidOperationException>(() => SendAsync(provider, new Rename("boom")));

        Assert.Equal(0, saves.Saved);
    }

    private static ServiceProvider Build(SaveCounter saves, bool validators = false, Assembly? moduleAssembly = null)
    {
        var services = new ServiceCollection()
            .AddMessaging()
            .AddSingleton(saves)
            .AddSingleton(new TestModule(moduleAssembly ?? typeof(PipelineBehaviorTests).Assembly))
            .AddScoped<IRequestHandler<Rename, Unit>, RenameHandler>()
            .AddScoped<IRequestHandler<Peek, int>, PeekHandler>()
            .AddScoped(typeof(IPipelineBehavior<,>), typeof(TestUnitOfWork<,>));
        if (validators)
            services.AddScoped<IValidator<Rename>, RenameValidator>();
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static async Task<T> SendAsync<T>(ServiceProvider provider, IRequest<T> request)
    {
        await using var scope = provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }
}
