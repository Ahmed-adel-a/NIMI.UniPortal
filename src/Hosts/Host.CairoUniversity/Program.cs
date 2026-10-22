using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Json;
using NIMI.UniPortal.Host.CairoUniversity;
using NIMI.UniPortal.Hosting;
using NIMI.UniPortal.Modules.Events.Api;
using NIMI.UniPortal.Modules.Forum.Api;
using NIMI.UniPortal.SharedKernel.Persistence;

// Cairo University: the core platform (both academic systems, chosen per department) + the premium modules it bought
// (Forum, Events). Its departments default to credit hours.
var builder = WebApplication.CreateBuilder(args);

// Developer-only settings (e.g. ConnectionStrings:Portal with the local password) in appsettings.Local.json:
// git-ignored and never copied into a Docker image. Environment variables and the command line still win.
if (builder.Environment.IsDevelopment())
{
    var sources = builder.Configuration.Sources;
    var firstEnvironmentSource = sources.TakeWhile(s => s is not EnvironmentVariablesConfigurationSource).Count();
    sources.Insert(firstEnvironmentSource, new JsonConfigurationSource
    {
        FileProvider = builder.Environment.ContentRootFileProvider,
        Path = "appsettings.Local.json",
        Optional = true,
        ReloadOnChange = true,
    });
}

builder.AddPortalCore();

// ── Premium modules Cairo bought (P11): Forum + Events — not Library ──
builder.Services.AddForumModule();
builder.Services.AddEventsModule();

// ── Demo campus (Development + Seed:DemoData only): Cairo's premium parts ──
builder.Services.AddScoped<IModuleSeeder, CairoDemoSeeder>();

var app = builder.Build();

await app.UsePortalCoreAsync();
app.MapForumEndpoints();
app.MapEventsEndpoints();

app.Run();
