using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stabilt.TellusTalk.Emulator;
using Stabilt.TellusTalk.Emulator.Http;
using Stabilt.TellusTalk.Emulator.Messages;
using Stabilt.TellusTalk.Emulator.Reports;
using Stabilt.TellusTalk.Emulator.Sending;

var builder = WebApplication.CreateBuilder(args);

// TellusTalk answers without a Server header.
builder.WebHost.ConfigureKestrel(kestrel => kestrel.AddServerHeader = false);

builder.Services.TryAddSingleton(TimeProvider.System);

// An enum travels as its name in camelCase, which is the read API's documented value: Scheduled is "scheduled", Ucs2 "ucs2".
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(
    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false)));

// In every environment: a request the read API cannot bind then ends in an empty 400 that ReadApiBadRequests fills,
// rather than, in Development, an exception that passes it by.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);

builder.Services
    .AddOptions<EmulatorOptions>()
    .BindConfiguration(EmulatorOptions.Section);

builder.Services
    .AddSingleton<MessageFactory>()
    .AddSingleton<MessageStore>()
    .AddHostedService<ScheduledRelease>();

builder.Services.AddReports();

builder.Services.AddSingleton<SendFailures>();

builder.Services
    .AddAuthentication(BasicAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(BasicAuthenticationHandler.SchemeName, configureOptions: null);

builder.Services.AddAuthorization();

builder.Services.AddRazorComponents();

builder.Services.AddHealthChecks();

var app = builder.Build();

// Without Cache-Control a browser guesses how long its copy stays fresh and can run an old script against a newer
// emulator; no-cache makes it ask first, which costs a 304.
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context => context.Context.Response.Headers.CacheControl = "no-cache",
});

app.Use(ReadApiBadRequests.Answer);

app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints();
app.MapHealthChecks("api/v1/health");

await app.RunAsync();
