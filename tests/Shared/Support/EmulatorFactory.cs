using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace Stabilt.TellusTalk.Emulator.Tests.Support;

/// <summary>The emulator in memory, on a clock the test moves.</summary>
public sealed class EmulatorFactory : WebApplicationFactory<Program>
{
    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 11, 7, 22, 0, 0, TimeSpan.Zero));

    /// <summary>Host settings, applied when the host is built: set them before the first request.</summary>
    public IDictionary<string, string?> Settings { get; } = new Dictionary<string, string?>(StringComparer.Ordinal);

    /// <summary>Where the emulator's delivery reports land.</summary>
    public ReportReceiver Reports { get; } = new();

    /// <summary>A send as a TellusTalk client makes one; <paramref name="user"/> <c>null</c> sends no credentials.</summary>
    public async Task<HttpResponseMessage> Send(string json, string? user = "tester", string contentType = "application/json")
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/send/v1", UriKind.Relative))
        {
            Content = new StringContent(json, Encoding.UTF8),
        };
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

        if (user is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:secret")));
        }

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        foreach (var (key, value) in Settings)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureServices(services =>
        {
            services.Replace(ServiceDescriptor.Singleton<TimeProvider>(Time));
            services
                .AddHttpClient("reports")
                .ConfigurePrimaryHttpMessageHandler(() => Reports);
        });
    }
}
