using NIMI.UniPortal.Host.CairoUniversity;
using NIMI.UniPortal.Hosting;
using NIMI.UniPortal.Hosting.DemoData;
using NIMI.UniPortal.Modules.Events.Api;
using NIMI.UniPortal.Modules.Forum.Api;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Api;
using NIMI.UniPortal.SharedKernel.Persistence;

// Cairo University: the core platform + the CreditHours lifecycle + the premium modules it bought (Forum, Events).
var builder = WebApplication.CreateBuilder(args);

builder.AddPortalCore();
builder.Services.AddCreditHoursModule();   // the one lifecycle variant this deployment composes

// ── Premium modules Cairo bought (P11): Forum + Events — not Library ──
builder.Services.AddForumModule();
builder.Services.AddEventsModule();

// ── Demo campus (Development + Seed:DemoData only): Cairo's lifecycle and premium parts ──
builder.Services.AddScoped<IDemoEnrollment, CreditHoursDemoEnrollment>();
builder.Services.AddScoped<IModuleSeeder, CairoDemoSeeder>();

var app = builder.Build();

await app.UsePortalCoreAsync();
app.MapCreditHoursEndpoints();
app.MapForumEndpoints();
app.MapEventsEndpoints();

app.Run();
